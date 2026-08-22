use crate::i18n;
use anyhow::{Context, Result};
use std::env;
use std::fs;
use std::io::{self, Write};
use std::path::PathBuf;
use std::process::Command;

use crate::build;
use crate::config::{Config, SelectedMod};

fn uploader_path() -> Option<PathBuf> {
    let home = env::var_os("HOME")?;

    #[cfg(target_os = "linux")]
    {
        let p = PathBuf::from(&home)
            .join(".local/share/Steam/steamapps/common/OxygenNotIncludedUploader/OniUploader64");
        if p.exists() {
            return Some(p);
        }
    }

    #[cfg(target_os = "macos")]
    {
        let p = PathBuf::from(&home)
            .join("Library/Application Support/Steam/steamapps/common/OxygenNotIncludedUploader/OxygenNotIncludedUploader.app/Contents/MacOS/OxygenNotIncludedUploader");
        if p.exists() {
            return Some(p);
        }
    }

    #[cfg(target_os = "windows")]
    {
        for p in [
            "C:\\Program Files (x86)\\Steam\\steamapps\\common\\OxygenNotIncludedUploader\\OxygenNotIncludedUploader.exe",
            "C:\\Program Files\\Steam\\steamapps\\common\\OxygenNotIncludedUploader\\OxygenNotIncludedUploader.exe",
        ] {
            let pb = PathBuf::from(p);
            if pb.exists() {
                return Some(pb);
            }
        }
    }

    None
}

fn has_steamcmd() -> bool {
    Command::new("sh")
        .args(["-c", "command -v steamcmd > /dev/null 2>&1"])
        .status()
        .map(|s| s.success())
        .unwrap_or(false)
}

fn generate_vdf(
    dist_mod: &PathBuf,
    preview: &PathBuf,
    title: &str,
    description: &str,
    changenote: &str,
    publishedfileid: &str,
) -> Result<PathBuf> {
    let vdf_path = dist_mod.join("workshop.vdf");
    let safe_title = escape_vdf_value(title);
    let safe_description = escape_vdf_value(description);
    let safe_changenote = escape_vdf_value(changenote);
    let content = format!(
        r#""workshopitem"
{{
	"appid"		"457140"
	"publishedfileid"	"{}"
	"contentfolder"	"{}"
	"previewfile"	"{}"
	"visibility"	"0"
	"title"		"{}"
	"description"	"{}"
	"changenote"	"{}"
}}
	"#,
        publishedfileid,
        dist_mod.to_string_lossy().replace('\\', "/"),
        preview.to_string_lossy().replace('\\', "/"),
        safe_title,
        safe_description,
        safe_changenote,
    );
    fs::write(&vdf_path, content)
        .with_context(|| i18n::err_vdf_write(vdf_path.display().to_string()))?;
    Ok(vdf_path)
}

fn read_mod_info(dist_mod: &PathBuf) -> Option<(String, String, String)> {
    // 从 mod.yaml 读取标题和描述（游戏运行时使用）
    let mut title = None;
    let mut desc = None;
    let yaml = fs::read_to_string(dist_mod.join("mod.yaml")).ok()?;
    for line in yaml.lines() {
        let line = line.trim();
        if let Some((k, v)) = line.split_once(':') {
            let k = k.trim();
            let v = v.trim().trim_matches('"').trim_matches('\'');
            if k == "title" {
                title = Some(v.to_string());
            } else if k == "description" {
                desc = Some(v.to_string());
            }
        }
    }
    // 从 mod_info.yaml 读取版本号
    let mut version = None;
    if let Ok(yaml2) = fs::read_to_string(dist_mod.join("mod_info.yaml")) {
        for line in yaml2.lines() {
            let line = line.trim();
            if let Some((k, v)) = line.split_once(':') {
                let k = k.trim();
                let v = v.trim().trim_matches('"').trim_matches('\'');
                if k == "version" {
                    version = Some(v.to_string());
                }
            }
        }
    }
    Some((
        title.unwrap_or_else(|| "Untitled Mod".to_string()),
        steam_markdown_description(dist_mod)
            .or_else(|| desc)
            .unwrap_or_default(),
        version.unwrap_or_else(|| "1.0.0".to_string()),
    ))
}

fn read_file(path: &PathBuf) -> Option<String> {
    fs::read_to_string(path).ok().map(|s| s.trim().to_string())
}

fn escape_vdf_value(value: &str) -> String {
    value
        .replace('\\', "\\\\")
        .replace('\"', "\\\"")
        .replace('\r', "")
        .replace('\n', "\\n")
}

fn steam_markdown_description(dist_mod: &PathBuf) -> Option<String> {
    let zh = read_file(&dist_mod.join("docs").join("steam-description-zh.md"));
    let en = read_file(&dist_mod.join("docs").join("steam-description-en.md"));
    match (zh, en) {
        (Some(zh_txt), Some(en_txt)) if !zh_txt.is_empty() && !en_txt.is_empty() => {
            Some(format!("{}\n\n{}", zh_txt, en_txt))
        }
        (Some(zh_txt), _) if !zh_txt.is_empty() => Some(zh_txt),
        (_, Some(en_txt)) if !en_txt.is_empty() => Some(en_txt),
        _ => None,
    }
}

fn extract_changelog_summary(project_dir: &PathBuf, max_items: usize) -> Option<String> {
    let changelog = project_dir.join("CHANGELOG.md");
    let content = fs::read_to_string(changelog).ok()?;
    let mut section_started = false;
    let mut entries: Vec<String> = Vec::new();

    for raw in content.lines() {
        let line = raw.trim();
        if line.starts_with("## ") {
            if section_started {
                break;
            }
            section_started = true;
            continue;
        }
        if !section_started {
            continue;
        }
        if line.is_empty() {
            continue;
        }
        if line.starts_with("- [") {
            let cleaned = line.trim_start_matches("- ").trim().to_string();
            entries.push(cleaned);
            if entries.len() >= max_items {
                break;
            }
        }
    }

    if entries.is_empty() {
        return extract_git_summary(project_dir, max_items);
    }

    Some(format!(
        "Auto changelog (latest {} items):\n{}",
        entries.len(),
        entries.join("\n")
    ))
}

fn extract_git_summary(project_dir: &PathBuf, max_items: usize) -> Option<String> {
    let max_items = max_items.to_string();
    let output = Command::new("git")
        .arg("-C")
        .arg(project_dir)
        .args([
            "log",
            "--max-count",
            &max_items,
            "--pretty=format:- %h %s",
            "--",
            ".",
        ])
        .output()
        .ok()?;

    if !output.status.success() {
        return None;
    }

    let lines = String::from_utf8_lossy(&output.stdout);
    let entries: Vec<&str> = lines
        .lines()
        .map(|line| line.trim())
        .filter(|line| !line.is_empty())
        .take(max_items.parse().ok()?)
        .collect();

    if entries.is_empty() {
        return None;
    }

    Some(format!(
        "Auto changelog (latest {} items):\n{}",
        entries.len(),
        entries.join("\n")
    ))
}

fn prompt(question: &str, default: Option<&str>) -> Result<String> {
    print!("{}", question);
    io::stdout().flush()?;
    let mut buf = String::new();
    io::stdin().read_line(&mut buf)?;
    let trimmed = buf.trim().to_string();
    if trimmed.is_empty() {
        Ok(default.unwrap_or("").to_string())
    } else {
        Ok(trimmed)
    }
}

pub fn run(cfg: &Config, selected: &SelectedMod, use_gui: bool, auto_note: bool) -> Result<()> {
    let repo_root = env::var_os("ONI_CLI_REPO_ROOT")
        .map(PathBuf::from)
        .unwrap_or_else(|| env::current_dir().unwrap());

    let assembly_name = selected.assembly_name(&repo_root);
    println!("🚀 {}", i18n::preparing_publish(selected.name.to_string()));
    build::run(cfg, selected, true)?;

    let dist_mod = cfg.dist_dir(&repo_root).join(&assembly_name);

    // 检查 mod_info.yaml
    let mod_info = dist_mod.join("mod_info.yaml");
    if !mod_info.exists() {
        println!("⚠️  {}", i18n::warn_no_mod_info());
    }

    // 检查预览图
    let preview_png = dist_mod.join("preview.png");
    let preview_jpg = dist_mod.join("preview.jpg");
    let preview = if preview_png.exists() {
        preview_png
    } else if preview_jpg.exists() {
        preview_jpg
    } else {
        println!("\n⚠️  {}", i18n::no_preview_image());
        println!("{}", i18n::preview_required());
        println!("{}", i18n::preview_suggestion(selected.config.project_abs(&repo_root).display().to_string()));
        anyhow::bail!("{}", i18n::err_missing_preview());
    };

    // 强制使用 GUI
    if use_gui {
        println!("\n📌 {}", i18n::forced_oniuploader());
        launch_uploader(&dist_mod, &preview);
        return Ok(());
    }

    // 尝试 SteamCMD 全自动上传
    if has_steamcmd() {
        println!("\n📡 {}", i18n::steamcmd_detected());
        let (title, desc, version) = read_mod_info(&dist_mod)
            .unwrap_or_else(|| (selected.name.clone(), String::new(), "1.0.0".to_string()));
        let project_dir = selected.config.project_abs(&repo_root);
        let auto_changenote = extract_changelog_summary(&project_dir, 6);
        let default_changenote = auto_changenote
            .clone()
            .unwrap_or_else(|| format!("Release {}", version));

        let changenote = if auto_note {
            default_changenote.clone()
        } else {
            prompt(
                &i18n::changelog_prompt(default_changenote.clone()),
                Some(default_changenote.as_str()),
            )?
        };

        let publishedfileid = if let Some(ref id) = selected.config.publishedfileid {
            println!("{}", i18n::using_configured_workshop_id(id.to_string()));
            id.clone()
        } else {
            let id = prompt(i18n::workshop_id_prompt(), Some("0"))?;
            if id.trim().is_empty() {
                "0".to_string()
            } else {
                id
            }
        };

        let vdf = generate_vdf(
            &dist_mod,
            &preview,
            &title,
            &desc,
            &changenote,
            &publishedfileid,
        )?;
        println!("\n📤 {}", i18n::upload_starting());
        println!("   vdf: {}", vdf.display());

        let steam_user = prompt(i18n::steam_username_prompt(), None)?;
        println!("\n📤 {}", i18n::uploading_wait());
        let output = Command::new("steamcmd")
            .args([
                "+login",
                &steam_user,
                "+workshop_build_item",
                &vdf.to_string_lossy(),
                "+quit",
            ])
            .output()
            .with_context(|| i18n::err_steamcmd_spawn())?;

        let stdout = String::from_utf8_lossy(&output.stdout);
        let stderr = String::from_utf8_lossy(&output.stderr);
        let full_output = format!("{} {}", stdout, stderr);

        if output.status.success() {
            // 尝试从输出中提取 Workshop ID
            let workshop_id =
                extract_workshop_id(&full_output).or_else(|| read_publishedfileid_from_vdf(&vdf));

            println!("✅ {}", i18n::upload_done());
            println!();

            if let Some(ref id) = workshop_id {
                println!("🔗 {}", i18n::workshop_link());
                println!(
                    "   https://steamcommunity.com/sharedfiles/filedetails/?id={}",
                    id
                );
                println!();
            } else if publishedfileid != "0" {
                println!("🔗 {}", i18n::workshop_link());
                println!(
                    "   https://steamcommunity.com/sharedfiles/filedetails/?id={}",
                    publishedfileid
                );
                println!();
            }

            println!("⚠️  {}", i18n::steamcmd_no_tags());
            println!("{}", i18n::tags_manual_1());
            println!("{}", i18n::tags_manual_2());
            println!("{}", i18n::tags_manual_3());
            println!("{}", i18n::tags_manual_4());
            println!();

            if workshop_id.is_none() && publishedfileid == "0" {
                println!("{}", i18n::first_upload_id_saved(vdf.display().to_string()));
            }
        } else {
            eprintln!("❌ {}", i18n::steamcmd_stderr(stderr.to_string()));
            println!("\n{}", i18n::fallback_to_gui());
            launch_uploader(&dist_mod, &preview);
        }

        return Ok(());
    }

    // 回退到 OniUploader GUI
    launch_uploader(&dist_mod, &preview);
    Ok(())
}

fn extract_workshop_id(output: &str) -> Option<String> {
    // SteamCMD 输出中可能包含 "PublishedFileId" 或数字 ID
    // 常见格式："PublishedFileId" "123456789" 或 Success. ID: 123456789
    for line in output.lines() {
        // 尝试匹配 "PublishedFileId" "12345"
        if let Some(pos) = line.find("PublishedFileId") {
            let rest = &line[pos..];
            if let Some(start) = rest.find('"').and_then(|s| rest[s + 1..].find('"')) {
                let after_first = &rest[start + 2..];
                if let Some(end) = after_first.find('"') {
                    let id = after_first[..end].trim();
                    if !id.is_empty() && id.chars().all(|c| c.is_ascii_digit()) {
                        return Some(id.to_string());
                    }
                }
            }
        }
        // 尝试匹配简单的数字 ID（8-12 位数字）
        for word in line.split_whitespace() {
            let clean = word.trim_matches(|c: char| !c.is_ascii_digit());
            if clean.len() >= 8 && clean.len() <= 12 && clean.chars().all(|c| c.is_ascii_digit()) {
                return Some(clean.to_string());
            }
        }
    }
    None
}

fn read_publishedfileid_from_vdf(vdf: &PathBuf) -> Option<String> {
    let content = fs::read_to_string(vdf).ok()?;
    for line in content.lines() {
        if line.contains("publishedfileid") {
            if let Some((_, val)) = line.split_once('"') {
                if let Some((_, val2)) = val.split_once('"') {
                    if let Some((id, _)) = val2.split_once('"') {
                        let id = id.trim();
                        if !id.is_empty() && id != "0" && id.chars().all(|c| c.is_ascii_digit()) {
                            return Some(id.to_string());
                        }
                    }
                }
            }
        }
    }
    None
}

fn launch_uploader(dist_mod: &PathBuf, preview: &PathBuf) {
    let uploader = match uploader_path() {
        Some(p) => p,
        None => {
            println!("\n❌ {}", i18n::no_upload_tool());
            println!("{}", i18n::option_one());
            println!("  Arch:    paru -S steamcmd");
            println!("  Ubuntu:  sudo apt install steamcmd");
            println!("{}", i18n::option_one_other());
            println!();
            println!("{}", i18n::option_two());
            return;
        }
    };

    println!("\n📤 {}", i18n::launching_oniuploader());
    println!("   {}", uploader.display());
    println!();
    println!("{}", i18n::follow_steps());
    println!("{}", i18n::step_add());
    println!("{}", i18n::step_mod_dir(dist_mod.display().to_string()));
    println!("{}", i18n::step_preview_ready(preview.display().to_string()));
    println!("{}", i18n::step_publish());
    println!();

    #[cfg(target_os = "linux")]
    {
        let _ = Command::new("xdg-open").arg(dist_mod).spawn();
    }
    #[cfg(target_os = "macos")]
    {
        let _ = Command::new("open").arg(dist_mod).spawn();
    }
    #[cfg(target_os = "windows")]
    {
        let _ = Command::new("explorer").arg(dist_mod).spawn();
    }

    let _ = Command::new(&uploader).spawn();
}
