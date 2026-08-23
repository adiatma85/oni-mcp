use crate::i18n;
use anyhow::{Context, Result};
use serde_json::Value;
use std::env;
use std::fs;
use std::path::{Path, PathBuf};
use std::process::Command;
use std::time::{SystemTime, UNIX_EPOCH};

use crate::archive;
use crate::build;
use crate::config::{Config, SelectedMod};

pub fn run(cfg: &Config, selected: &SelectedMod) -> Result<()> {
    let repo_root = env::var_os("ONI_CLI_REPO_ROOT")
        .map(PathBuf::from)
        .unwrap_or_else(|| env::current_dir().unwrap());

    // 1. 构建
    build::run(cfg, selected, false)?;

    // 2. 安装到 Dev 目录
    let dist = cfg.dist_dir(&repo_root);
    let assembly_name = selected.assembly_name(&repo_root);
    let zip = dist.join(format!("{}.zip", assembly_name));

    if !zip.exists() {
        anyhow::bail!("{}", i18n::err_artifact_missing(zip.display().to_string()));
    }

    let dev_dir = cfg.dev_mod_dir(&selected.name)?;
    let legacy_dev_dir = cfg.legacy_dev_mod_dir(&selected.name)?;
    println!("\n🔧 {}", i18n::installing_dev(dev_dir.display().to_string()));

    if dev_dir.exists() {
        fs::remove_dir_all(&dev_dir)
            .with_context(|| i18n::err_clean_dev(dev_dir.display().to_string()))?;
    }
    if legacy_dev_dir != dev_dir && legacy_dev_dir.exists() {
        fs::remove_dir_all(&legacy_dev_dir)
            .with_context(|| i18n::err_clean_dev_lower(legacy_dev_dir.display().to_string()))?;
    }
    fs::create_dir_all(&dev_dir)
        .with_context(|| i18n::err_create_dev(dev_dir.display().to_string()))?;

    archive::unzip(&zip, &dev_dir)?;
    enable_dev_mod(cfg, selected, &dev_dir)?;

    println!("✅ {}", i18n::dev_installed());

    if is_game_running() {
        println!("⚠️  {}", i18n::dev_restart_needed());
    } else {
        println!("💡 {}", i18n::dev_not_running());
    }

    Ok(())
}

fn enable_dev_mod(cfg: &Config, selected: &SelectedMod, dev_dir: &Path) -> Result<()> {
    let static_id = read_static_id(dev_dir).unwrap_or_else(|| format!("local.{}", selected.name));
    let mods_file = cfg.game_mods_dir()?.join("mods.json");
    if !mods_file.exists() {
        println!("⚠️  {}", i18n::mods_json_absent(selected.name.to_string()));
        return Ok(());
    }

    let content = fs::read_to_string(&mods_file)
        .with_context(|| i18n::err_mods_json_read(mods_file.display().to_string()))?;
    let mut data: Value = serde_json::from_str(&content)
        .with_context(|| i18n::err_mods_json_parse(mods_file.display().to_string()))?;
    let (before_enabled, after_enabled) = {
        let mods = data
            .get_mut("mods")
            .and_then(Value::as_array_mut)
            .with_context(|| i18n::err_mods_json_no_array())?;
        let before_enabled = count_enabled_mods(mods);

        let mut found = false;
        for item in mods.iter_mut() {
            let same_static_id = item
                .get("staticID")
                .and_then(Value::as_str)
                .map(|value| value == static_id)
                .unwrap_or(false);
            let same_dev_id = item
                .get("label")
                .and_then(|label| label.get("id"))
                .and_then(Value::as_str)
                .map(|value| value == selected.name)
                .unwrap_or(false);
            if same_static_id || same_dev_id {
                item["enabled"] = Value::Bool(true);
                item["status"] = Value::Number(1.into());
                ensure_expansion_enabled(item);
                found = true;
            }
        }

        if !found {
            mods.push(new_dev_mod_entry(selected, &static_id));
        }

        (before_enabled, count_enabled_mods(mods))
    };
    data["mod_load_in_progress"] = Value::Bool(false);
    if after_enabled < before_enabled {
        anyhow::bail!("{}", i18n::err_mods_json_would_drop(before_enabled, after_enabled));
    }
    if after_enabled <= 1 {
        println!("⚠️  {}", i18n::warn_few_enabled_mods(after_enabled));
    }

    let formatted = serde_json::to_string_pretty(&data)?;
    let backup = mods_file.with_extension(format!(
        "json.onim-backup-{}",
        SystemTime::now()
            .duration_since(UNIX_EPOCH)
            .map(|duration| duration.as_secs())
            .unwrap_or(0)
    ));
    fs::write(&backup, &content)
        .with_context(|| i18n::err_mods_json_backup(backup.display().to_string()))?;
    fs::write(&mods_file, format!("{}\n", formatted))
        .with_context(|| i18n::err_mods_json_write(mods_file.display().to_string()))?;
    println!(
        "✅ {}",
        i18n::dev_enabled_in_mods_json(static_id, before_enabled, after_enabled, backup.display().to_string())
    );
    Ok(())
}

fn count_enabled_mods(mods: &[Value]) -> usize {
    mods.iter()
        .filter(|item| {
            item.get("enabled").and_then(Value::as_bool) == Some(true)
                || item.get("status").and_then(Value::as_i64) == Some(1)
        })
        .count()
}

fn read_static_id(dev_dir: &Path) -> Option<String> {
    let content = fs::read_to_string(dev_dir.join("mod.yaml")).ok()?;
    content.lines().find_map(|line| {
        let (key, value) = line.split_once(':')?;
        if key.trim() != "staticID" {
            return None;
        }
        let cleaned = value
            .trim()
            .trim_matches('"')
            .trim_matches('\'')
            .to_string();
        if cleaned.is_empty() {
            None
        } else {
            Some(cleaned)
        }
    })
}

fn ensure_expansion_enabled(item: &mut Value) {
    let Some(array) = item.get_mut("enabledForDlc").and_then(Value::as_array_mut) else {
        item["enabledForDlc"] = Value::Array(vec![Value::String("EXPANSION1_ID".to_string())]);
        return;
    };
    if !array
        .iter()
        .any(|value| value.as_str() == Some("EXPANSION1_ID"))
    {
        array.push(Value::String("EXPANSION1_ID".to_string()));
    }
}

fn new_dev_mod_entry(selected: &SelectedMod, static_id: &str) -> Value {
    serde_json::json!({
     "label": {
      "distribution_platform": 4,
      "id": selected.name,
      "title": selected.name,
      "version": 0
     },
     "status": 1,
     "enabled": true,
     "enabledForDlc": ["EXPANSION1_ID"],
     "crash_count": 0,
     "reinstall_path": null,
     "staticID": static_id
    })
}

fn is_game_running() -> bool {
    // Match the game process name only. Never use full-command-line matching
    // (e.g. `pgrep -f`): Steam uploader paths like
    // `.../OxygenNotIncludedUploader/OniUploader64` would false-positive.
    #[cfg(target_os = "linux")]
    {
        process_name_running(&["OxygenNotIncluded", "OxygenNotIncluded.x86_64"])
    }

    #[cfg(target_os = "windows")]
    {
        Command::new("tasklist")
            .arg("/FI")
            .arg("IMAGENAME eq OxygenNotIncluded.exe")
            .output()
            .map(|o| String::from_utf8_lossy(&o.stdout).contains("OxygenNotIncluded.exe"))
            .unwrap_or(false)
    }

    #[cfg(target_os = "macos")]
    {
        // The macOS bundle executable is `Contents/MacOS/Oxygen Not Included`, with spaces.
        // `pgrep -x OxygenNotIncluded` never matches it, so onim reported the game as stopped
        // while it was running. Exact matching still keeps OniUploader64 from false-positiving.
        process_name_running(&["Oxygen Not Included", "OxygenNotIncluded"])
    }
}

/// True if any of the exact process names is running (`pgrep -x`).
#[cfg(any(target_os = "linux", target_os = "macos"))]
fn process_name_running(names: &[&str]) -> bool {
    names.iter().any(|name| {
        Command::new("pgrep")
            .args(["-x", name])
            .output()
            .map(|o| o.status.success())
            .unwrap_or(false)
    })
}
