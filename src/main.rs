use anyhow::{Result, bail};
use clap::{Parser, Subcommand, ValueEnum};
use std::path::PathBuf;

#[macro_use]
mod i18n;

mod archive;
mod build;
mod config;
mod dev;
mod doctor;
mod info;
mod init;
mod install;
mod publish;
mod setup;
mod uninstall;

#[derive(Parser)]
#[command(name = "onim")]
#[command(about = "Oxygen Not Included mod development CLI")]
#[command(version)]
struct Cli {
    #[command(subcommand)]
    command: Commands,

    /// Path to the config file
    #[arg(short, long, global = true)]
    config: Option<PathBuf>,

    /// Mod to operate on (defaults to the configured default, or the first one)
    #[arg(short, long, global = true)]
    r#mod: Option<String>,
}

#[derive(Subcommand)]
enum Commands {
    /// Initialise project config (detect the game path, write config files)
    Setup,
    /// Create a new mod from the template
    Init {
        /// Mod name (ASCII, no spaces)
        name: String,
        /// Author name
        #[arg(short, long)]
        author: Option<String>,
        /// Mod description
        #[arg(short, long)]
        desc: Option<String>,
        /// Mod version
        #[arg(long, default_value = "0.1.7")]
        mod_version: String,
    },
    /// Build a mod (Debug by default; --release for Release)
    Build {
        #[arg(short, long)]
        release: bool,
        /// Build every configured mod
        #[arg(long)]
        all: bool,
    },
    /// Dev mode: build and install into the game's Dev folder
    Dev {
        /// Build and install every configured mod in dev mode
        #[arg(long)]
        all: bool,
    },
    /// Release install into the game's Local folder
    Install,
    /// Uninstall a mod from the game folder
    Uninstall {
        /// Uninstall scope
        #[arg(short, long, value_enum, default_value_t = UninstallScope::All)]
        scope: UninstallScope,
    },
    /// Show installed mod information
    Info,
    /// Publish to the Steam Workshop
    Publish {
        /// Force the OniUploader GUI instead of SteamCMD
        #[arg(long)]
        gui: bool,
        /// Use the latest changelog entry as the upload note, without prompting
        #[arg(long)]
        auto_note: bool,
    },
    /// List every configured mod
    List,
    /// Check the local ONI mod development environment
    Doctor,
}

#[derive(Clone, ValueEnum)]
pub enum UninstallScope {
    Dev,
    Local,
    All,
}

fn main() -> Result<()> {
    let cli = Cli::parse();

    // Setup, Init and Doctor do not need a strictly loaded config.
    match &cli.command {
        Commands::Setup => return setup::run(),
        Commands::Init {
            name,
            author,
            desc,
            mod_version,
        } => {
            return init::run(
                name.clone(),
                author.clone(),
                desc.clone(),
                mod_version.clone(),
                cli.config,
            );
        }
        Commands::Doctor => return doctor::run(cli.config.clone()),
        _ => {}
    }

    let cfg = config::load(cli.config)?;

    match cli.command {
        Commands::Build { release, all } => {
            if all {
                if cli.r#mod.is_some() {
                    bail!("{}", i18n::err_all_and_mod_conflict());
                }
                for selected in cfg.select_all_mods()? {
                    build::run(&cfg, &selected, release)?;
                }
                Ok(())
            } else {
                let selected = cfg.select_mod(cli.r#mod)?;
                build::run(&cfg, &selected, release)
            }
        }
        Commands::Dev { all } => {
            if all {
                if cli.r#mod.is_some() {
                    bail!("{}", i18n::err_all_and_mod_conflict());
                }
                for selected in cfg.select_all_mods()? {
                    dev::run(&cfg, &selected)?;
                }
                Ok(())
            } else {
                let selected = cfg.select_mod(cli.r#mod)?;
                dev::run(&cfg, &selected)
            }
        }
        Commands::Install => {
            let selected = cfg.select_mod(cli.r#mod)?;
            install::run(&cfg, &selected)
        }
        Commands::Uninstall { scope } => {
            let selected = cfg.select_mod(cli.r#mod)?;
            uninstall::run(&cfg, &selected, scope)
        }
        Commands::Info => info::run(&cfg),
        Commands::Publish { gui, auto_note } => {
            let selected = cfg.select_mod(cli.r#mod)?;
            publish::run(&cfg, &selected, gui, auto_note)
        }
        Commands::List => {
            println!("{}", i18n::configured_mods());
            let default = cfg.default_mod.clone();
            for (name, m) in &cfg.mods {
                let marker = if Some(name.to_string()) == default {
                    i18n::default_marker()
                } else {
                    ""
                };
                println!("  • {}  ({}){}", name, m.path.display(), marker);
            }
            Ok(())
        }
        _ => unreachable!(),
    }
}
