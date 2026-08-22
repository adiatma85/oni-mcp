use crate::i18n;
use anyhow::{Context, Result};
use std::env;
use std::path::PathBuf;
use std::process::Command;

use crate::config::{Config, SelectedMod};

pub fn run(cfg: &Config, selected: &SelectedMod, release: bool) -> Result<()> {
    let repo_root = env::var_os("ONI_CLI_REPO_ROOT")
        .map(PathBuf::from)
        .unwrap_or_else(|| env::current_dir().unwrap());

    let mod_project = selected.config.project_abs(&repo_root);
    println!("📦 {}", i18n::building_mod(selected.name.to_string(), mod_project.display().to_string()));

    let mut cmd = Command::new("dotnet");
    cmd.arg("build").current_dir(&mod_project);

    if release {
        cmd.args(["-c", "Release"]);
        println!("{}", i18n::mode_release());
    } else {
        println!("{}", i18n::mode_debug());
    }

    let status = cmd
        .status()
        .with_context(|| i18n::err_dotnet_build_spawn())?;

    if !status.success() {
        anyhow::bail!("{}", i18n::err_dotnet_build_failed());
    }

    // 显示产物
    let dist = cfg.dist_dir(&repo_root);
    let assembly_name = selected.assembly_name(&repo_root);
    let zip = dist.join(format!("{}.zip", assembly_name));
    let src = dist.join(format!("{}-src.tar.gz", assembly_name));

    println!("✅ {}", i18n::build_ok());
    if zip.exists() {
        println!("   📁 {}", zip.display());
    }
    if src.exists() {
        println!("   📁 {}", src.display());
    }

    Ok(())
}
