use crate::i18n;
use anyhow::{Context, Result};
use std::env;
use std::fs;
use std::path::PathBuf;

use crate::archive;
use crate::build;
use crate::config::{Config, SelectedMod};

pub fn run(cfg: &Config, selected: &SelectedMod) -> Result<()> {
    let repo_root = match env::var_os("ONI_CLI_REPO_ROOT") {
        Some(path) => PathBuf::from(path),
        None => env::current_dir().context("读取当前目录失败")?,
    };

    // 1. 构建 Release
    build::run(cfg, selected, true)?;

    // 2. 安装到 Local 目录
    let dist = cfg.dist_dir(&repo_root);
    let assembly_name = selected.assembly_name(&repo_root);
    let zip = dist.join(format!("{}.zip", assembly_name));

    if !zip.exists() {
        anyhow::bail!("{}", i18n::err_artifact_missing(zip.display().to_string()));
    }

    let local_dir = cfg.local_mod_dir(&selected.name)?;
    println!("\n📥 {}", i18n::installing_local(local_dir.display().to_string()));

    if local_dir.exists() {
        fs::remove_dir_all(&local_dir)
            .with_context(|| i18n::err_clean_local(local_dir.display().to_string()))?;
    }
    fs::create_dir_all(&local_dir)
        .with_context(|| i18n::err_create_local(local_dir.display().to_string()))?;

    archive::unzip(&zip, &local_dir)?;

    println!("✅ {}", i18n::install_done());
    println!("{}", i18n::enable_in_game(selected.name.to_string()));

    Ok(())
}
