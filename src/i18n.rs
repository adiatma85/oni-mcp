//! CLI message catalogue.
//!
//! `onim` was Chinese-only, which made first-run setup unreadable for anyone else: the very
//! first thing a new user meets is a Chinese prompt asking for the game directory.
//!
//! Language is resolved once, in this order:
//!   1. `ONIM_LANG` (`en` / `zh`) — explicit override, wins over everything
//!   2. `lang = "..."` in `onim.toml` — per-project preference
//!   3. `LC_ALL` / `LC_MESSAGES` / `LANG` — anything starting with `zh` selects Chinese
//!   4. English
//!
//! Note this makes English the default where the CLI previously always spoke Chinese.
//! Chinese users on a `zh_*` locale still get Chinese automatically, and `ONIM_LANG=zh`
//! restores it anywhere.
//!
//! Messages are declared with the `msg!` macro so both translations sit on adjacent lines
//! and cannot drift apart unnoticed. Parameterised messages use *named* placeholders, so a
//! translation is free to reorder them; Rust rejects a translation that drops an argument,
//! which keeps the two languages structurally in sync.

use std::env;
use std::fs;
use std::sync::OnceLock;

#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum Lang {
    En,
    Zh,
}

fn parse_lang(value: &str) -> Option<Lang> {
    let v = value.trim().to_ascii_lowercase();
    if v.is_empty() {
        return None;
    }
    if v.starts_with("zh") || v == "cn" || v == "chinese" {
        return Some(Lang::Zh);
    }
    if v.starts_with("en") || v == "english" {
        return Some(Lang::En);
    }
    None
}

/// Best-effort read of `lang` from onim.toml without pulling in the full config loader,
/// which needs a resolved game path and would fail during first-run setup.
fn lang_from_config() -> Option<Lang> {
    let content = fs::read_to_string("onim.toml").ok()?;
    for line in content.lines() {
        let trimmed = line.trim();
        if trimmed.starts_with('[') {
            break; // `lang` is a top-level key; stop at the first table header.
        }
        if let Some(rest) = trimmed.strip_prefix("lang") {
            if let Some((_, value)) = rest.split_once('=') {
                return parse_lang(value.trim().trim_matches('"').trim_matches('\''));
            }
        }
    }
    None
}

fn detect() -> Lang {
    if let Ok(value) = env::var("ONIM_LANG") {
        if let Some(lang) = parse_lang(&value) {
            return lang;
        }
    }
    if let Some(lang) = lang_from_config() {
        return lang;
    }
    for key in ["LC_ALL", "LC_MESSAGES", "LANG"] {
        if let Ok(value) = env::var(key) {
            if value.trim().to_ascii_lowercase().starts_with("zh") {
                return Lang::Zh;
            }
        }
    }
    Lang::En
}

pub fn lang() -> Lang {
    static CACHE: OnceLock<Lang> = OnceLock::new();
    *CACHE.get_or_init(detect)
}

macro_rules! msg {
    ($name:ident, $en:expr, $zh:expr $(,)?) => {
        pub fn $name() -> &'static str {
            match $crate::i18n::lang() {
                $crate::i18n::Lang::En => $en,
                $crate::i18n::Lang::Zh => $zh,
            }
        }
    };
    ($name:ident($($arg:ident: $t:ty),+ $(,)?), $en:expr, $zh:expr $(,)?) => {
        pub fn $name($($arg: $t),+) -> String {
            match $crate::i18n::lang() {
                $crate::i18n::Lang::En => format!($en, $($arg = $arg),+),
                $crate::i18n::Lang::Zh => format!($zh, $($arg = $arg),+),
            }
        }
    };
}

// ---------------------------------------------------------------- shared / main

msg!(err_all_and_mod_conflict, "--all cannot be combined with -m/--mod", "--all 与 -m/--mod 不可同时使用");
msg!(configured_mods, "Configured mods:", "已配置的 Mod：");
msg!(default_marker, " <- default", " ← 默认");

// ---------------------------------------------------------------- setup

msg!(setup_title, "onim project setup", "onim 项目初始化");
msg!(checking_deps, "Checking dependencies...", "检查依赖...");
msg!(deps_missing_header, "Missing required tools:", "缺少必要工具：");
msg!(install_and_retry, "Install them and try again:", "请安装后重试：");
msg!(unzip_hint, "  unzip: sudo apt install unzip  (or your package manager)", "  unzip: sudo apt install unzip  (或对应包管理器)");
msg!(err_missing_deps, "missing dependencies", "缺少依赖");
msg!(deps_ok, "All dependencies installed", "所有依赖已安装");
msg!(verifying_game_files, "Verifying game files...", "验证游戏文件...");
msg!(problems_found, "Problems found:", "发现问题：");
msg!(verify_failed_continue, "Verification failed. Continue anyway? [y/N] ", "文件验证未通过，是否仍继续？ [y/N] ");
msg!(err_setup_cancelled, "setup cancelled", "初始化已取消");
msg!(all_files_verified, "All required files verified", "所有关键文件验证通过");
msg!(path_correct_prompt, "Is this path correct? [Y/n] ", "路径是否正确？ [Y/n] ");
msg!(use_this_path_prompt, "Use this path? [Y/n] ", "使用此路径？ [Y/n] ");
msg!(enter_game_dir, "Enter the Oxygen Not Included install directory (the level containing the game executable):", "请输入缺氧游戏安装目录（包含 OxygenNotIncluded 可执行文件的那一层）：");
msg!(err_no_game_path, "no game path provided, setup cancelled", "未提供游戏路径，初始化取消");
msg!(setup_done, "Setup complete!", "初始化完成！");
msg!(next_steps, "Next steps:", "下一步：");
msg!(next_build, "  onim build       build the default mod", "  onim build       构建默认 Mod");
msg!(next_init, "  onim init <name> create a new mod", "  onim init <name> 创建新 Mod");
msg!(desc_game_dll, "main game logic DLL", "游戏主逻辑 DLL");
msg!(desc_harmony, "Harmony patching framework", "Harmony 补丁框架");
msg!(desc_unity_core, "Unity engine core", "Unity 引擎核心");
msg!(err_game_path_unset, "Game path is not configured!", "游戏路径未配置！");

msg!(missing_required_file(file: &str, desc: &str, path: &str),
     "missing {file} ({desc}) at {path}",
     "缺少 {file}（{desc}）在 {path}");
msg!(found_existing_path(path: String), "Found configured game path: {path}", "检测到现有游戏路径：{path}");
msg!(auto_detected_path(path: String), "Auto-detected game path: {path}", "自动检测到游戏路径：{path}");
msg!(kept_existing_config(file: &str, count: usize),
     "Kept existing {file} ({count} mod entries left untouched)",
     "保留现有 {file}（{count} 个 Mod 配置未改动）");
msg!(config_unparseable_backed_up(file: &str, backup: String),
     "Existing {file} could not be parsed or declares no mods; backed up to {backup}",
     "现有 {file} 无法解析或不含 Mod，已备份到 {backup}");
msg!(writing_file(file: &str), "Writing {file} ...", "写入 {file} ...");
msg!(err_write_failed(file: &str), "failed to write {file}", "写入 {file} 失败");
msg!(err_backup_failed(file: &str), "failed to back up {file}", "备份 {file} 失败");
msg!(out_game_path(path: String), "   Game path:     {path}", "   游戏路径：{path}");
msg!(out_config_path(path: String), "   Config file:   {path}", "   配置文件：{path}");
msg!(out_props_path(path: String), "   MSBuild props: {path}", "   MSBuild 配置：{path}");

// ---------------------------------------------------------------- doctor

msg!(doctor_title, "onim health check", "onim 健康检查");
msg!(label_config_file, "onim config file", "onim 配置文件");
msg!(label_game_path, "ONI game path", "ONI 游戏路径");
msg!(label_managed_dir, "Managed assemblies directory", "托管程序集目录");
msg!(label_mods_root, "Game mods root", "游戏 Mod 根目录");
msg!(external_tools, "External tools:", "外部工具：");
msg!(configured_mod_sources, "Configured mod sources:", "已配置 Mod 源码：");
msg!(no_mods_configured, "   No mods configured", "   未配置任何 Mod");
msg!(err_no_mods_configured, "no mods configured", "未配置任何 Mod");
msg!(config_unreadable_for_mods, "   Config file could not be parsed; mod source paths unavailable", "   配置文件无法解析，无法读取 Mod 源码路径");
msg!(doctor_pass, "Environment check passed", "环境检查通过");
msg!(game_path_unresolvable, "ONI game path and Managed directory: could not resolve from Directory.Build.props", "ONI 游戏路径与托管程序集目录：无法从 Directory.Build.props 解析");

msg!(mods_root_unresolvable(error: String), "Game mods root: could not resolve ({error})", "游戏 Mod 根目录：无法解析（{error}）");
msg!(err_mods_root_unresolvable(error: String), "game mods root could not be resolved: {error}", "游戏 Mod 根目录无法解析：{error}");
msg!(tool_not_on_path(tool: &str), "   {tool} (not found on PATH)", "   {tool}（PATH 中未找到）");
msg!(err_tool_missing(tool: &str), "missing external tool: {tool}", "缺少外部工具：{tool}");
msg!(doctor_fail(count: usize), "Environment check failed ({count} issue(s)):", "环境检查失败（{count} 项）：");
msg!(err_doctor_issues(count: usize), "onim doctor found {count} environment issue(s)", "onim doctor 发现 {count} 项环境问题");
msg!(path_not_a_dir(label: String, path: String), "{label} does not exist or is not a directory: {path}", "{label} 不存在或不是目录：{path}");
msg!(path_missing(label: String, path: String), "{label} does not exist: {path}", "{label} 不存在：{path}");

// Label separator: Chinese uses the fullwidth colon, English a plain one with a space.
msg!(sep, ": ", "：");

// Comments written into a freshly generated onim.toml.
msg!(toml_header, "# onim config file", "# onim 配置文件");
msg!(toml_game_path_note, "# The game path is managed in Directory.Build.props", "# 游戏路径在 Directory.Build.props 中统一管理");
msg!(toml_default_mod_note, "# Default mod (used when -m is omitted)", "# 默认 Mod（不指定 -m 时使用）");
