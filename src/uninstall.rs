use crate::i18n;
use anyhow::{Context, Result};
use std::fs;

use crate::UninstallScope;
use crate::config::{Config, SelectedMod};

pub fn run(cfg: &Config, selected: &SelectedMod, scope: UninstallScope) -> Result<()> {
    let dev_dir = cfg.dev_mod_dir(&selected.name)?;
    let legacy_dev_dir = cfg.legacy_dev_mod_dir(&selected.name)?;
    let local_dir = cfg.local_mod_dir(&selected.name)?;

    let mut uninstalled = vec![];

    match scope {
        UninstallScope::Dev | UninstallScope::All => {
            if dev_dir.exists() {
                fs::remove_dir_all(&dev_dir)
                    .with_context(|| i18n::err_uninstall_dev(dev_dir.display().to_string()))?;
                uninstalled.push(i18n::removed_dev(selected.name.to_string()));
            }
            if legacy_dev_dir != dev_dir && legacy_dev_dir.exists() {
                fs::remove_dir_all(&legacy_dev_dir).with_context(|| {
                    i18n::err_uninstall_dev_lower(legacy_dev_dir.display().to_string())
                })?;
                uninstalled.push(i18n::removed_dev_lower(selected.name.to_string()));
            }
        }
        _ => {}
    }

    match scope {
        UninstallScope::Local | UninstallScope::All => {
            if local_dir.exists() {
                fs::remove_dir_all(&local_dir)
                    .with_context(|| i18n::err_uninstall_local(local_dir.display().to_string()))?;
                uninstalled.push(i18n::removed_local(selected.name.to_string()));
            }
        }
        _ => {}
    }

    if uninstalled.is_empty() {
        println!("ℹ️  {}", i18n::not_installed_anywhere(selected.name.to_string()));
    } else {
        println!("✅ {}", i18n::uninstalled(selected.name.to_string()));
        for msg in uninstalled {
            println!("   {}", msg);
        }
    }

    Ok(())
}
