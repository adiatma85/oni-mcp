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

// Windows-only messages are cfg-gated: they are genuinely used there, and without the gate
// every other platform reports them as dead code.

// ---------------------------------------------------------------- archive

#[cfg(target_os = "windows")]
msg!(err_unzip_powershell, "extraction failed (PowerShell Expand-Archive)", "解压失败（PowerShell Expand-Archive）");
msg!(err_unzip_cli, "extraction failed (unzip); make sure unzip is installed", "解压失败（unzip），请确认已安装 unzip");
#[cfg(target_os = "windows")]
msg!(err_unzip_powershell_status(status: String), "extraction failed (PowerShell Expand-Archive, exit status: {status})", "解压失败（PowerShell Expand-Archive，退出状态：{status}）");
msg!(err_unzip_cli_status(status: String), "extraction failed (unzip, exit status: {status})", "解压失败（unzip，退出状态：{status}）");

// ---------------------------------------------------------------- build

msg!(mode_release, "   Mode: Release", "   模式: Release");
msg!(mode_debug, "   Mode: Debug", "   模式: Debug");
msg!(err_dotnet_build_spawn, "failed to run dotnet build; make sure the .NET SDK is installed", "执行 dotnet build 失败，请确认已安装 .NET SDK");
msg!(err_dotnet_build_failed, "dotnet build failed", "dotnet build 失败");
msg!(build_ok, "Build succeeded!", "构建成功！");
msg!(building_mod(name: String, key: String), "Building mod: {name} ({key})", "构建 Mod: {name} ({key})");

// ---------------------------------------------------------------- config

msg!(err_no_cwd, "could not determine the current working directory", "无法获取当前工作目录");
msg!(err_no_config_dir, "could not determine the directory holding the onim config file", "无法确定 onim 配置文件所在目录");
msg!(err_no_home, "could not read the HOME environment variable", "无法获取 HOME 环境变量");
#[cfg(target_os = "windows")]
msg!(err_win_docs_api, "could not obtain the Documents directory via the Windows Known Folder API", "无法通过 Windows Known Folder API 获取 Documents 目录");
#[cfg(target_os = "windows")]
msg!(err_win_docs_utf8, "the Documents directory returned by the Windows Known Folder API is not valid UTF-8", "Windows Known Folder API 返回的 Documents 目录不是有效 UTF-8");
#[cfg(target_os = "windows")]
msg!(err_win_docs_empty, "the Windows Known Folder API returned an empty Documents directory", "Windows Known Folder API 返回了空的 Documents 目录");

msg!(err_props_missing(file: String), "{file} not found; run `onim setup` to initialise the project first", "找不到 {file}，请先运行 `onim setup` 初始化项目");
msg!(err_read_failed(file: String), "failed to read {file}", "读取 {file} 失败");
msg!(err_game_path_empty(file: String), "OniGamePath in {file} is empty", "{file} 中的 OniGamePath 为空");
msg!(err_game_path_tag_missing(file: String), "no <OniGamePath> tag in {file}; run `onim setup` first", "{file} 中找不到 <OniGamePath> 标签，请先运行 `onim setup`");
msg!(err_config_missing_at(path: String), "config file does not exist: {path}", "配置文件不存在：{path}");
msg!(err_config_read(path: String, error: String), "failed to read the config file: {path} ({error})", "读取配置文件失败：{path}（{error}）");
msg!(err_config_parse(path: String, error: String), "failed to parse the config file: {path} ({error})", "解析配置文件失败：{path}（{error}）");
msg!(err_file_missing(file: String), "{file} not found", "找不到 {file}");
msg!(err_read_failed_with(file: String, error: String), "failed to read {file}: {error}", "读取 {file} 失败：{error}");
msg!(err_no_mods_in(file: String), "no mods configured; add a [mods.XXX] section to {file}", "没有配置任何 Mod，请在 {file} 中添加 [mods.XXX]");
msg!(err_config_absent(path: String), "config missing: {path}", "配置缺失：{path}");
msg!(err_mod_not_found(name: String, known: String), "mod '{name}' not found. Configured mods: {known}\nUse -m with one of those names.", "找不到 Mod '{name}'，已配置的 Mod：{known}\n请用 -m 指定正确的名称。");
msg!(err_game_path_missing(path: String), "game path does not exist: {path}\nRun `onim setup` to reconfigure", "游戏路径不存在：{path}\n请运行 `onim setup` 重新配置");
msg!(err_assembly_missing(path: String, expected: String), "Assembly-CSharp.dll not found; the game path may be wrong: {path}\nExpected at: {expected}\nRun `onim setup` to reconfigure", "找不到 Assembly-CSharp.dll，游戏路径可能不正确：{path}\n期望位置：{expected}\n请运行 `onim setup` 重新配置");
msg!(err_config_read_simple(path: String), "failed to read the config file: {path}", "读取配置文件失败：{path}");
msg!(err_config_parse_simple(path: String), "failed to parse the config file: {path}", "解析配置文件失败：{path}");
msg!(err_mod_path_missing(name: String, path: String, file: String, key: String), "path for mod '{name}' does not exist: {path}\nCheck the `path` under [mods.{key}] in {file}", "Mod '{name}' 的路径不存在：{path}\n请在 {file} 中检查 [mods.{key}] 的 path");
#[cfg(target_os = "windows")]
msg!(err_win_docs_failed(code: String, detail: String), "the Windows Known Folder API failed to return the Documents directory ({code}): {detail}", "通过 Windows Known Folder API 获取 Documents 目录失败（{code}）：{detail}");

// ---------------------------------------------------------------- dev / install / uninstall

msg!(dev_installed, "Installed into the game's Dev folder", "已安装到游戏 Dev 目录");
msg!(dev_restart_needed, "Game is running; restart it to load the new build", "检测到游戏正在运行，需要重启游戏才能加载新版本的 Mod");
msg!(dev_not_running, "Game is not running; enable the mod in the Mods list after launching", "游戏未运行，启动游戏后在 Mod 列表中启用即可");
msg!(err_mods_json_no_array, "mods.json has no `mods` array", "mods.json 缺少 mods 数组");
msg!(install_done, "Installed into the game's Local folder", "已正式安装到游戏 Local 目录");

msg!(err_artifact_missing(path: String), "build artifact not found: {path}", "找不到构建产物：{path}");
msg!(installing_dev(path: String), "Installing into Dev: {path}", "安装到 Dev 目录：{path}");
msg!(installing_local(path: String), "Installing into Local: {path}", "安装到 Local 目录：{path}");
msg!(err_clean_dev(path: String), "failed to clean the old Dev folder: {path}", "清理旧 Dev 目录失败：{path}");
msg!(err_clean_dev_lower(path: String), "failed to clean the legacy lowercase dev folder: {path}", "清理旧版小写 dev 目录失败：{path}");
msg!(err_create_dev(path: String), "failed to create the Dev folder: {path}", "创建 Dev 目录失败：{path}");
msg!(err_clean_local(path: String), "failed to clean the old Local folder: {path}", "清理旧 Local 目录失败：{path}");
msg!(err_create_local(path: String), "failed to create the Local folder: {path}", "创建 Local 目录失败：{path}");
msg!(enable_in_game(name: String), "   Launch the game -> Mods list -> enable '{name}'", "   启动游戏 → Mod 列表 → 启用 '{name}'");
msg!(mods_json_absent(name: String), "mods.json not found; you will still need to enable '{name}' after first launching the game", "未找到 mods.json，首次进游戏后仍需确认启用 '{name}'");
msg!(err_mods_json_read(error: String), "failed to read mods.json: {error}", "读取 mods.json 失败：{error}");
msg!(err_mods_json_parse(error: String), "failed to parse mods.json: {error}", "解析 mods.json 失败：{error}");
msg!(err_mods_json_backup(error: String), "failed to back up mods.json: {error}", "备份 mods.json 失败：{error}");
msg!(err_mods_json_write(error: String), "failed to write mods.json: {error}", "写入 mods.json 失败：{error}");
msg!(dev_enabled_in_mods_json(id: String, before: usize, after: usize, backup: String), "Enabled Dev mod in mods.json: {id} (enabled {before} -> {after}, backup: {backup})", "已在 mods.json 启用 Dev mod：{id}（启用数 {before} -> {after}，备份：{backup}）");

msg!(err_uninstall_dev(path: String), "failed to uninstall from Dev: {path}", "卸载 Dev 目录失败：{path}");
msg!(err_uninstall_dev_lower(path: String), "failed to uninstall from the legacy lowercase dev folder: {path}", "卸载旧版小写 dev 目录失败：{path}");
msg!(err_uninstall_local(path: String), "failed to uninstall from Local: {path}", "卸载 Local 目录失败：{path}");
msg!(removed_dev(name: String), "Dev/{name} removed", "Dev/{name} 已删除");
msg!(removed_dev_lower(name: String), "dev/{name} removed (legacy incorrect folder)", "dev/{name} 已删除（旧版错误目录）");
msg!(removed_local(name: String), "Local/{name} removed", "Local/{name} 已删除");
msg!(not_installed_anywhere(name: String), "'{name}' is not installed in any folder", "'{name}' 没有安装在任何目录中");
msg!(uninstalled(name: String), "Uninstalled '{name}':", "已卸载 '{name}'：");

// ---------------------------------------------------------------- info

msg!(no_mods_installed, "No installed mods detected", "没有检测到任何已安装的 Mod");
msg!(game_mods_dir(path: String), "Game mods folder: {path}", "游戏 Mod 目录：{path}");
msg!(section_dev(count: usize), "[Dev] development mods ({count})", "[Dev] 开发测试 Mod ({count} 个)");
msg!(section_dev_lower(count: usize), "[dev] legacy lowercase folder, not scanned by ONI on Linux ({count})", "[dev] 旧版小写目录（ONI 在 Linux 上不会扫描，{count} 个）");
msg!(section_local(count: usize), "[Local] locally installed mods ({count})", "[Local] 本地安装 Mod ({count} 个)");
msg!(section_steam(count: usize), "[Steam] Workshop mods ({count})", "[Steam] 创意工坊 Mod ({count} 个)");
msg!(no_mod_yaml(name: String), "   - {name} (no mod.yaml)", "   • {name} (无 mod.yaml)");

// ---------------------------------------------------------------- init

msg!(err_rename_csproj, "failed to rename the .csproj", "重命名 .csproj 失败");
msg!(init_next_steps, "Next steps:", "下一步：");

msg!(err_read_file(path: String), "failed to read file: {path}", "读取文件失败：{path}");
msg!(err_write_file(path: String), "failed to write file: {path}", "写入文件失败：{path}");
msg!(err_template_missing(path: String, dir: String), "template directory does not exist: {path}\nMake sure {dir} exists", "模板目录不存在：{path}\n请确认 {dir} 目录存在");
msg!(err_mod_dir_exists(path: String), "mod directory already exists: {path}", "Mod 目录已存在：{path}");
msg!(creating_mod(name: String), "Creating new mod: {name}", "创建新 Mod: {name}");
msg!(out_namespace(ns: String), "   Namespace: {ns}", "   命名空间: {ns}");
msg!(out_version(version: String), "   Version:   {version}", "   版本: {version}");
msg!(err_update_failed(file: String), "failed to update {file}", "更新 {file} 失败");
msg!(appended_to(file: String), "   Appended to {file}", "   已追加到 {file}");
msg!(mod_created(name: String), "Mod '{name}' created!", "Mod '{name}' 创建成功！");
msg!(out_directory(path: String), "   Directory: {path}", "   目录：{path}");
msg!(next_build_mod(name: String), "  onim build -m {name}      build it", "  onim build -m {name}      构建");
msg!(next_dev_mod(name: String), "  onim dev -m {name}        build and install for testing", "  onim dev -m {name}        开发测试");
msg!(err_copy_file(from: String, to: String), "failed to copy file: {from} -> {to}", "复制文件失败：{from} -> {to}");

// ---------------------------------------------------------------- publish

msg!(warn_no_mod_info, "Warning: mod_info.yaml not found", "警告：找不到 mod_info.yaml");
msg!(no_preview_image, "No preview image found (preview.png / preview.jpg)", "未找到预览图 (preview.png / preview.jpg)");
msg!(preview_required, "   Both OniUploader and SteamCMD require a preview image.", "   OniUploader / SteamCMD 都需要预览图。");
msg!(err_missing_preview, "missing preview image", "缺少预览图");
msg!(forced_oniuploader, "Forced to use the OniUploader GUI", "已强制指定使用 OniUploader GUI");
msg!(steamcmd_detected, "steamcmd detected: fully automated upload available", "检测到 steamcmd，支持全自动上传！");
msg!(workshop_id_prompt, "Existing Workshop ID? (leave blank for a first upload): ", "已有 Workshop ID？（首次上传留空）: ");
msg!(upload_starting, "Starting upload...", "开始上传...");
msg!(steam_username_prompt, "Steam username: ", "Steam 用户名: ");
msg!(uploading_wait, "Uploading, please wait...", "正在上传，请稍候...");
msg!(err_steamcmd_spawn, "failed to start steamcmd", "启动 steamcmd 失败");
msg!(upload_done, "Upload complete!", "上传完成！");
msg!(workshop_link, "Steam Workshop link:", "Steam 创意工坊链接：");
msg!(steamcmd_no_tags, "Important: SteamCMD uploads cannot set Tags (categories)!", "重要提示：SteamCMD 上传无法设置 Tags（类别）！");
msg!(tags_manual_1, "   Add them manually on the Workshop page:", "   请前往 Steam 创意工坊页面手动补充：");
msg!(tags_manual_2, "   1. Sign in to Steam -> Workshop -> Your Items", "   1. 登录 Steam → 创意工坊 → 你的物品", );
msg!(tags_manual_3, "   2. Edit the mod -> add Tags (Buildings, Quality of Life, ...)", "   2. 编辑 Mod → 添加 Tags（如 Buildings、Quality of Life 等）");
msg!(tags_manual_4, "   3. Save changes", "   3. 保存更改");
msg!(fallback_to_gui, "Falling back to the OniUploader GUI...", "回退到 OniUploader GUI...");
msg!(no_upload_tool, "No upload tool found!", "找不到上传工具！");
msg!(option_one, "Option 1 (recommended): install steamcmd for fully automated uploads", "方案一（推荐）：安装 steamcmd 实现全自动上传");
msg!(option_one_other, "  other:   https://developer.valvesoftware.com/wiki/SteamCMD", "  其他:    https://developer.valvesoftware.com/wiki/SteamCMD");
msg!(option_two, "Option 2: from the Steam library -> Tools -> install 'Oxygen Not Included Uploader'", "方案二：从 Steam 库 → 工具 → 安装 'Oxygen Not Included Uploader'");
msg!(launching_oniuploader, "Launching OniUploader...", "启动 OniUploader...");
msg!(follow_steps, "Follow these steps:", "请按以下步骤操作：");
msg!(step_add, "  1. Click 'Add' to add a new mod", "  1. 点击 'Add' 添加新 Mod");
msg!(step_publish, "  4. Fill in the details and click 'Publish'", "  4. 填写信息后点击 'Publish'");

msg!(err_vdf_write(error: String), "failed to write the vdf: {error}", "写入 vdf 失败：{error}");
msg!(preparing_publish(name: String), "Preparing to publish: {name}...", "准备发布：{name}...");
msg!(preview_suggestion(dir: String), "   Suggestion: put a preview.png in {dir}", "   建议：在 {dir} 下放一张 preview.png");
msg!(changelog_prompt(default: String), "Release note [default: {default}]: ", "更新说明 [默认: {default}]: ");
msg!(using_configured_workshop_id(id: String), "   Using the Workshop ID from config: {id}", "   使用配置中的 Workshop ID: {id}");
msg!(first_upload_id_saved(file: String), "   First upload: the Workshop ID was written to {file} and will be reused next time.", "   首次上传，Workshop ID 已写入 {file}，下次可直接更新。");
msg!(steamcmd_stderr(output: String), "steamcmd error output:\n{output}", "steamcmd 错误输出：\n{output}");
msg!(step_mod_dir(path: String), "  2. Mod directory: {path}", "  2. Mod 目录选择：{path}");
msg!(step_preview_ready(path: String), "  3. Preview image ready: {path}", "  3. 预览图已就绪：{path}");

msg!(err_mods_json_would_drop(before: usize, after: usize), "refusing to write mods.json: enabled mod count would drop from {before} to {after}", "拒绝写入 mods.json：启用的 Mod 数量会从 {before} 降到 {after}");
msg!(warn_few_enabled_mods(count: usize), "mods.json currently has only {count} enabled mod(s). If this save depends on other mods, restore them in the ONI Mods menu before loading the save.", "mods.json 当前只有 {count} 个已启用 Mod。如果存档依赖其他 Mod，请先在游戏 Mod 菜单中恢复后再读档。");
