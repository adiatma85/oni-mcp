use anyhow::{Result, bail};
use std::env;
use std::fs;
use std::path::{Path, PathBuf};

use crate::config;
use crate::i18n;

pub fn run(explicit_config_path: Option<PathBuf>) -> Result<()> {
    let diagnostics = config::read_doctor_diagnostics(explicit_config_path)?;
    let mut issues = Vec::new();

    println!("🩺 {}\n", i18n::doctor_title());
    check_config(
        i18n::label_config_file(),
        &diagnostics.config_path,
        diagnostics.config_error.as_deref(),
        &mut issues,
    );
    check_config(
        "Directory.Build.props",
        &diagnostics.props_path,
        diagnostics.game_path_error.as_deref(),
        &mut issues,
    );

    match (&diagnostics.game_path, &diagnostics.managed_path) {
        (Some(game_path), Some(managed_path)) => {
            check_directory(i18n::label_game_path(), game_path, &mut issues);
            check_directory(i18n::label_managed_dir(), managed_path, &mut issues);
            check_file(
                "Assembly-CSharp.dll",
                &managed_path.join("Assembly-CSharp.dll"),
                &mut issues,
            );
        }
        _ => println!("❌ {}", i18n::game_path_unresolvable()),
    }

    match (&diagnostics.game_mods_dir, &diagnostics.game_mods_dir_error) {
        (Some(path), None) => check_directory(i18n::label_mods_root(), path, &mut issues),
        (_, Some(error)) => {
            println!("❌ {}", i18n::mods_root_unresolvable(error.to_string()));
            issues.push(i18n::err_mods_root_unresolvable(error.to_string()));
        }
        _ => unreachable!(),
    }

    println!("\n🔧 {}", i18n::external_tools());
    for tool in required_tools() {
        if tool_on_path(tool) {
            println!("   ✅ {tool}");
        } else {
            println!("❌ {}", i18n::tool_not_on_path(tool));
            issues.push(i18n::err_tool_missing(tool));
        }
    }

    println!("\n📦 {}", i18n::configured_mod_sources());
    match diagnostics.mods {
        Some(mods) if mods.is_empty() => {
            println!("   ❌ {}", i18n::no_mods_configured().trim());
            issues.push(i18n::err_no_mods_configured().to_string());
        }
        Some(mods) => {
            let mut mods: Vec<_> = mods.iter().collect();
            mods.sort_unstable_by_key(|(key, _)| *key);
            for (key, mod_cfg) in mods {
                let source = mod_cfg.project_abs(&diagnostics.repo_root);
                check_directory(
                    &format!("Mod {} ({})", mod_cfg.mod_name(key), key),
                    &source,
                    &mut issues,
                );
            }
        }
        None => println!("   ❌ {}", i18n::config_unreadable_for_mods().trim()),
    }

    if issues.is_empty() {
        println!("\n✅ {}", i18n::doctor_pass());
        Ok(())
    } else {
        println!("\n❌ {}", i18n::doctor_fail(issues.len()));
        for issue in &issues {
            println!("   • {issue}");
        }
        bail!("{}", i18n::err_doctor_issues(issues.len()));
    }
}

fn check_config(label: &str, path: &Path, error: Option<&str>, issues: &mut Vec<String>) {
    match error {
        Some(error) => {
            println!("❌ {label}{}{}（{error}）", i18n::sep(), path.display());
            issues.push(error.to_string());
        }
        None => println!("✅ {label}{}{}", i18n::sep(), path.display()),
    }
}

fn check_directory(label: &str, path: &Path, issues: &mut Vec<String>) {
    if path.is_dir() {
        println!("✅ {label}{}{}", i18n::sep(), path.display());
    } else {
        println!("❌ {label}{}{}", i18n::sep(), path.display());
        issues.push(i18n::path_not_a_dir(label.to_string(), path.display().to_string()));
    }
}

fn check_file(label: &str, path: &Path, issues: &mut Vec<String>) {
    if path.is_file() {
        println!("✅ {label}{}{}", i18n::sep(), path.display());
    } else {
        println!("❌ {label}{}{}", i18n::sep(), path.display());
        issues.push(i18n::path_missing(label.to_string(), path.display().to_string()));
    }
}

fn required_tools() -> &'static [&'static str] {
    #[cfg(target_os = "windows")]
    {
        &["dotnet", "tar"]
    }

    #[cfg(not(target_os = "windows"))]
    {
        &["dotnet", "tar", "unzip"]
    }
}

fn tool_on_path(tool: &str) -> bool {
    let Some(paths) = env::var_os("PATH") else {
        return false;
    };
    let file_name = format!("{tool}{}", env::consts::EXE_SUFFIX);
    env::split_paths(&paths).any(|directory| is_executable(&directory.join(&file_name)))
}

fn is_executable(path: &Path) -> bool {
    if !path.is_file() {
        return false;
    }

    #[cfg(unix)]
    {
        use std::os::unix::fs::PermissionsExt;

        fs::metadata(path).is_ok_and(|metadata| metadata.permissions().mode() & 0o111 != 0)
    }

    #[cfg(not(unix))]
    {
        true
    }
}

#[cfg(test)]
mod tests {
    use super::required_tools;

    #[cfg(target_os = "windows")]
    #[test]
    fn required_tools_should_use_windows_archive_support() {
        assert_eq!(required_tools(), ["dotnet", "tar"]);
    }

    #[cfg(not(target_os = "windows"))]
    #[test]
    fn required_tools_should_include_unzip_off_windows() {
        assert_eq!(required_tools(), ["dotnet", "tar", "unzip"]);
    }
}
