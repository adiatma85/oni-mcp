use crate::i18n;
use anyhow::{Context, Result};
use std::env;
use std::fs;
use std::io::{self, Write};
use std::path::{Path, PathBuf};
use std::process::Command;

const CONFIG_FILE: &str = "onim.toml";
const BUILD_PROPS: &str = "Directory.Build.props";

fn check_command(cmd: &str) -> bool {
    Command::new("sh")
        .args(["-c", &format!("command -v {} > /dev/null 2>&1", cmd)])
        .status()
        .map(|s| s.success())
        .unwrap_or(false)
}

fn prompt(question: &str, default: Option<&str>) -> Result<String> {
    print!("{}", question);
    io::stdout().flush()?;
    let mut buf = String::new();
    io::stdin().read_line(&mut buf)?;
    let trimmed = buf.trim().to_string();
    if trimmed.is_empty() {
        if let Some(d) = default {
            Ok(d.to_string())
        } else {
            Ok(trimmed)
        }
    } else {
        Ok(trimmed)
    }
}

fn auto_detect() -> Option<PathBuf> {
    let home = env::var_os("HOME")?;

    #[cfg(target_os = "linux")]
    {
        let p = PathBuf::from(&home).join(".local/share/Steam/steamapps/common/OxygenNotIncluded");
        if p.join("OxygenNotIncluded_Data/Managed/Assembly-CSharp.dll")
            .exists()
        {
            return Some(p);
        }
    }

    #[cfg(target_os = "macos")]
    {
        let p = PathBuf::from(&home)
            .join("Library/Application Support/Steam/steamapps/common/OxygenNotIncluded");
        if crate::config::managed_path_candidates(&p)
            .iter()
            .any(|m| m.join("Assembly-CSharp.dll").exists())
        {
            return Some(p);
        }
    }

    #[cfg(target_os = "windows")]
    {
        for base in [
            "C:\\Program Files (x86)\\Steam\\steamapps\\common\\OxygenNotIncluded",
            "C:\\Program Files\\Steam\\steamapps\\common\\OxygenNotIncluded",
        ] {
            let p = PathBuf::from(base);
            if p.join("OxygenNotIncluded_Data\\Managed\\Assembly-CSharp.dll")
                .exists()
            {
                return Some(p);
            }
        }
    }

    None
}

/// Number of mods in an existing, parseable `onim.toml`.
/// None when the file is absent, unparseable, or declares no mods, i.e. when writing the
/// default template would not destroy anything the user cares about.
fn existing_mod_count(path: &Path) -> Option<usize> {
    let content = fs::read_to_string(path).ok()?;
    let config: crate::config::Config = toml::from_str(&content).ok()?;
    if config.mods.is_empty() {
        None
    } else {
        Some(config.mods.len())
    }
}

fn backup_path(path: &Path) -> PathBuf {
    let stamp = std::time::SystemTime::now()
        .duration_since(std::time::UNIX_EPOCH)
        .map(|d| d.as_secs())
        .unwrap_or(0);
    let name = path
        .file_name()
        .map(|n| n.to_string_lossy().to_string())
        .unwrap_or_else(|| CONFIG_FILE.to_string());
    path.with_file_name(format!("{}.onim-backup-{}", name, stamp))
}

fn validate_game_path(path: &PathBuf) -> Result<Vec<String>> {
    let mut errors = vec![];
    let managed = crate::config::managed_path_for_game(path);

    let required = [
        ("Assembly-CSharp.dll", i18n::desc_game_dll()),
        ("0Harmony.dll", i18n::desc_harmony()),
        ("UnityEngine.CoreModule.dll", i18n::desc_unity_core()),
    ];

    for (file, desc) in &required {
        let p = managed.join(file);
        if !p.exists() {
            errors.push(i18n::missing_required_file(file, desc, &p.display().to_string()));
        }
    }

    Ok(errors)
}

pub fn run() -> Result<()> {
    println!("🛠️  {}\n", i18n::setup_title());

    // 检查依赖
    println!("📋 {}", i18n::checking_deps());
    let mut missing = vec![];

    if !check_command("dotnet") {
        missing.push("dotnet (.NET SDK)");
    }
    #[cfg(not(target_os = "windows"))]
    {
        if !check_command("unzip") {
            missing.push("unzip");
        }
    }
    if !check_command("tar") {
        missing.push("tar");
    }

    if !missing.is_empty() {
        println!("⚠️  {}", i18n::deps_missing_header());
        for m in &missing {
            println!("   ❌ {}", m);
        }
        println!();
        println!("{}", i18n::install_and_retry());
        println!("  .NET SDK: https://dotnet.microsoft.com/download");
        #[cfg(not(target_os = "windows"))]
        {
            println!("{}", i18n::unzip_hint());
        }
        anyhow::bail!("{}", i18n::err_missing_deps());
    }
    println!("✅ {}\n", i18n::deps_ok());

    let cwd = env::current_dir()?;
    let config_path = cwd.join(CONFIG_FILE);
    let props_path = cwd.join(BUILD_PROPS);

    // 1. 检查/询问游戏路径
    let mut game_path = None;

    if config_path.exists() {
        let content = fs::read_to_string(&config_path).unwrap_or_default();
        for line in content.lines() {
            if line.trim_start().starts_with("game_path") {
                if let Some((_, val)) = line.split_once('=') {
                    let p = val.trim().trim_matches('"').trim_matches('\'');
                    let pb = PathBuf::from(p);
                    if pb.exists() {
                        game_path = Some(pb);
                    }
                }
            }
        }
    }

    if let Some(ref path) = game_path {
        println!("{}", i18n::found_existing_path(path.display().to_string()));
        let answer = prompt(i18n::path_correct_prompt(), Some("Y"))?;
        if answer.eq_ignore_ascii_case("n") {
            game_path = None;
        }
    }

    if game_path.is_none() {
        if let Some(detected) = auto_detect() {
            println!("\n{}", i18n::auto_detected_path(detected.display().to_string()));
            let answer = prompt(i18n::use_this_path_prompt(), Some("Y"))?;
            if answer.eq_ignore_ascii_case("n") {
                game_path = None;
            } else {
                game_path = Some(detected);
            }
        }
    }

    if game_path.is_none() {
        println!("\n{}", i18n::enter_game_dir());
        let input = prompt("> ", None)?;
        if input.is_empty() {
            anyhow::bail!("{}", i18n::err_no_game_path());
        }
        game_path = Some(PathBuf::from(input));
    }

    let game_path = game_path.unwrap();

    // 2. 验证
    println!("\n🔍 {}", i18n::verifying_game_files());
    let errors = validate_game_path(&game_path)?;
    if !errors.is_empty() {
        println!("⚠️  {}", i18n::problems_found());
        for e in &errors {
            println!("   - {}", e);
        }
        let answer = prompt(i18n::verify_failed_continue(), Some("N"))?;
        if !answer.eq_ignore_ascii_case("y") {
            anyhow::bail!("{}", i18n::err_setup_cancelled());
        }
    } else {
        println!("✅ {}", i18n::all_files_verified());
    }

    // 3. 写入 onim.toml（只包含 Mod 列表，游戏路径在 Directory.Build.props 中）
    let toml_content = format!(
        "{header}\n{game_path_note}\n\n{default_mod_note}\ndefault_mod = \"OniModTemplate\"\n\n[mods.OniModTemplate]\npath = \"mods/OniModTemplate\"\n",
        header = i18n::toml_header(),
        game_path_note = i18n::toml_game_path_note(),
        default_mod_note = i18n::toml_default_mod_note()
    );

    // setup 负责游戏路径和依赖，Mod 列表由用户维护。
    // 这里以前无条件覆盖 onim.toml，会抹掉已有的 [mods.*]、default_mod 和
    // publishedfileid（Steam 创意工坊 ID 丢失后只能手动找回），所以先保留已有配置。
    println!();
    match existing_mod_count(&config_path) {
        Some(count) => {
            println!("↩️  {}", i18n::kept_existing_config(CONFIG_FILE, count));
        }
        None => {
            if config_path.exists() {
                let backup = backup_path(&config_path);
                fs::copy(&config_path, &backup)
                    .with_context(|| i18n::err_backup_failed(CONFIG_FILE))?;
                println!("⚠️  {}", i18n::config_unparseable_backed_up(CONFIG_FILE, backup.display().to_string()));
            }
            println!("📝 {}", i18n::writing_file(CONFIG_FILE));
            fs::write(&config_path, &toml_content)
                .with_context(|| i18n::err_write_failed(CONFIG_FILE))?;
        }
    }

    // 4. 写入 Directory.Build.props
    // Managed 目录按平台实际布局解析后写成绝对路径：
    // macOS 的 .app 包把游戏数据放在 Contents/Resources/Data，和 Windows/Linux 不同，
    // 直接用 $(OniGamePath)/OxygenNotIncluded_Data/Managed 拼接会得到不存在的路径。
    let managed_path = crate::config::managed_path_for_game(&game_path);
    let props_content = format!(
        r#"<Project>

  <!-- Generated by onim setup -->

  <PropertyGroup>
    <OniGamePath>{}</OniGamePath>
  </PropertyGroup>

  <PropertyGroup>
    <OniManagedPath>{}</OniManagedPath>
  </PropertyGroup>

  <Target Name="ValidateOniGamePath" BeforeTargets="BeforeBuild">
    <Error Text="{game_path_unset}" Condition="'$(OniGamePath)' == ''" />
  </Target>

</Project>
"#,
        game_path.to_string_lossy().replace('"', "&quot;"),
        managed_path.to_string_lossy().replace('"', "&quot;"),
        game_path_unset = i18n::err_game_path_unset()
    );

    println!("📝 {}", i18n::writing_file(BUILD_PROPS));
    fs::write(&props_path, props_content).with_context(|| i18n::err_write_failed(BUILD_PROPS))?;

    println!("\n✅ {}", i18n::setup_done());
    println!("{}", i18n::out_game_path(game_path.display().to_string()));
    println!("{}", i18n::out_config_path(config_path.display().to_string()));
    println!("{}", i18n::out_props_path(props_path.display().to_string()));
    println!();
    println!("{}", i18n::next_steps());
    println!("{}", i18n::next_build());
    println!("{}", i18n::next_init());

    Ok(())
}
