import {
  commands,
  env,
  type ExtensionContext,
  ExtensionMode,
  l10n,
  QuickPickItemKind,
  StatusBarAlignment,
  type StatusBarItem,
  Uri,
  window,
  workspace,
  type WorkspaceConfiguration,
} from "vscode";
import * as net from "net";
import * as fs from "fs";
import * as os from "os";
import {
  LanguageClient,
  type LanguageClientOptions,
  type ServerOptions,
  type StreamInfo,
  TransportKind,
} from "vscode-languageclient/node";
import * as path from "path";
import { TraitView } from "./views/TraitsView";
import { TelemetryReporter } from "@vscode/extension-telemetry";
import { ModifierQuerierView } from "./views/ModifierQuerierView";
import { CharacterEditorView } from './views/CharacterEditorView';

let client: LanguageClient;
let analyzeAllFilesEnd = false;
const connectionString =
  "InstrumentationKey=48ff3211-ba0a-4751-b903-322194147eab;IngestionEndpoint=https://eastasia-0.in.applicationinsights.azure.com/;LiveEndpoint=https://eastasia.livediagnostics.monitor.azure.com/;ApplicationId=623c90cc-047d-42f9-9632-c3c575bd0e6d";

export async function activate(context: ExtensionContext) {
  const reporter = new TelemetryReporter(connectionString);
  context.subscriptions.push(reporter);

  try {
    reporter.sendTelemetryEvent("activate", {
      language: env.language,
    });
  } catch (error) {
    console.log(error);
  }

  const statusBarItem = window.createStatusBarItem(StatusBarAlignment.Right, 100000);
  const openLogs = commands.registerCommand("vmoder.openLogs", () => {
    const dirPath = path.dirname(command);
    commands.executeCommand("revealFileInOS", Uri.file(path.join(dirPath, "Logs")));

    reporter.sendTelemetryEvent("openLogs");
  });

  context.subscriptions.push(openLogs, statusBarItem);

  let serverOptions: ServerOptions;
  let command = "";
  if (context.extensionMode == ExtensionMode.Development) {
    const connectionInfo = {
      port: 1231,
    };
    serverOptions = () => {
      // Connect to language server via socket
      const socket = net.connect(connectionInfo);
      const result: StreamInfo = {
        writer: socket,
        reader: socket as NodeJS.ReadableStream,
      };
      socket.on("close", () => {
        console.log("client connect error!");
      });
      return Promise.resolve(result);
    };
  } else {
    const platform: string = os.platform();

    switch (platform) {
      case "win32":
        command = path.join(context.extensionPath, "server", "win-x64", "VModer.Core.exe");
        break;
      case "linux":
        command = path.join(context.extensionPath, "server", "linux-x64", "VModer.Core");
        fs.chmodSync(command, "755");
        break;
      case "darwin":
        command = path.join(context.extensionPath, "server", "osx-x64", "VModer.Core");
        break;
    }
    console.log("command: " + command);

    serverOptions = {
      run: { command: command, args: [], transport: TransportKind.stdio },
      debug: { command: command, args: [] },
    };
  }

  const config = workspace.getConfiguration();
  const gameRootFolderPath =
    config.get<string>("VModer.GameRootPath") || config.get<string>("cwtools.cache.hoi4");

  if (gameRootFolderPath === undefined || gameRootFolderPath === "") {
    await pickGameRootFolderPath(config);
  }

  // 控制语言客户端的选项
  const clientOptions: LanguageClientOptions = {
    documentSelector: [{ scheme: "file", language: "hoi4" }],
    initializationOptions: {
      GameRootFolderPath: gameRootFolderPath,
      Blacklist: config.get<string[]>("VModer.Blacklist") || [],
      ErrorCodeBlacklist: config.get<string[]>("VModer.ErrorCodeBlacklist") || [],
      ParseFileMaxSize: config.get<number>("VModer.ParseFileMaxSize") || 2,
      GameLanguage: config.get<string>("VModer.GameLocalizedLanguage") || "default",
      ExtensionPath: context.extensionPath,
    },
  };

  // 创建语言客户端并启动客户端。
  client = new LanguageClient("vmoder", "VModer Server", serverOptions, clientOptions);

  client.onNotification("analyzeAllFilesStart", () => {
    statusBarItem.text = "$(loading~spin) VModer Analyzing";
  });

  client.onNotification("analyzeAllFilesEnd", () => {
    analyzeAllFilesEnd = true;
  });

  const isOpenWorkspace = workspace.workspaceFolders !== undefined;
  if (isOpenWorkspace) {
    await client.start();
    setInterval(() => {
      updateStatusBarServerInfo(statusBarItem, client);
    }, config.get<number>("VModer.RamQueryIntervalTime") || 1500);

    client.info(l10n.t("ServerStarting"));
  } else {
    client.info(l10n.t("UnableStart"));
  }

  statusBarItem.command = "vmoder.openMenu";
  statusBarItem.show();
  updateStatusBarItem(statusBarItem, client);
  const clearImageCache = commands.registerCommand("vmoder.clearImageCache", () => {
    client.sendNotification("clearImageCache");
    reporter.sendTelemetryEvent("clearImageCache");
  });

  const openTraitsView = commands.registerCommand("vmoder.openTraitsView", () => {
    if (!client.isRunning()) {
      return;
    }

    TraitView.render(context, client);
    reporter.sendTelemetryEvent("openTraitsView");
  });

  const openModifierQuerierView = commands.registerCommand("vmoder.openModifierQuerierView", () => {
    if (!client.isRunning()) {
      return;
    }

    ModifierQuerierView.render(context, client);
    reporter.sendTelemetryEvent("openModifierQuerierView");
  });

  const openCharcaterEditor = commands.registerCommand("vmoder.openCharacterEditor", () => {
    if (!client.isRunning()) {
      return;
    }

    CharacterEditorView.render(context, client);
    reporter.sendTelemetryEvent("openCharacterEditorView");
  });

  const openMenu = createMenu(config, reporter);

  context.subscriptions.push(
    client.onDidChangeState(() => updateStatusBarItem(statusBarItem, client)),
    clearImageCache,
    openTraitsView,
    openCharcaterEditor,
    openMenu,
    openModifierQuerierView
  );
}

async function pickGameRootFolderPath(config: WorkspaceConfiguration) {
  await window.showWarningMessage(l10n.t("SelectGameRootPath"), l10n.t("SelectFolder"));
  const uri = await window.showOpenDialog({
    canSelectFiles: false,
    canSelectFolders: true,
    canSelectMany: false,
    openLabel: l10n.t("SelectFolder"),
  });
  if (uri && uri[0]) {
    await config.update("VModer.GameRootPath", uri[0].fsPath, true);
    await window.showInformationMessage(l10n.t("MustRestart"));
  }
}

function updateStatusBarItem(statusBarItem: StatusBarItem, client: LanguageClient) {
  if (client.isRunning()) {
    statusBarItem.text = "$(notebook-state-success) VModer";
    statusBarItem.tooltip = "VModer is running";
  } else {
    statusBarItem.text = "$(extensions-warning-message) VModer";
    statusBarItem.tooltip = "VModer is stopped";
  }
}

async function updateStatusBarServerInfo(statusBarItem: StatusBarItem, client: LanguageClient) {
  if (client.isRunning() && analyzeAllFilesEnd) {
    const info: { memoryUsedBytes: number } = await client.sendRequest("getRuntimeInfo");
    statusBarItem.text =
      "$(notebook-state-success) VModer RAM " + formatBytes(info.memoryUsedBytes);
  }
}

function formatBytes(bytes: number): string {
  if (!Number.isFinite(bytes) || bytes <= 0) {
    return "0 Bytes";
  }
  const k = 1024;
  const sizes = ["Bytes", "KB", "MB", "GB", "TB"];
  const i = Math.floor(Math.log(bytes) / Math.log(k));
  const size = (bytes / Math.pow(k, i)).toFixed(2);
  return `${size} ${sizes[i]}`;
}

function createMenu(config: WorkspaceConfiguration, reporter: TelemetryReporter) {
  const openMenu = commands.registerCommand("vmoder.openMenu", async () => {
    reporter.sendTelemetryEvent("openStateBarMenu");
    const options = [
      {
        label: l10n.t("Menu.OpenTraitsView"),
        description: l10n.t("Menu.OpenTraitsViewDesc"),
        action: "openTraitsView",
      },
      {
        label: l10n.t("Menu.OpenModifierQuerierView"),
        description: l10n.t("Menu.OpenModifierQuerierViewDesc"),
        action: "openModifierQuerierView",
      },
      // {
      //   label: l10n.t("Menu.OpenCharacterEditorView"),
      //   description: l10n.t("Menu.OpenCharacterEditorViewDesc"),
      //   action: "openCharacterEditor",
      // },
      {
        kind: QuickPickItemKind.Separator,
        label: ""
      },
      {
        label: l10n.t("Menu.ClearImageCache"),
        description: l10n.t("Menu.ClearImageCacheDesc"),
        action: "clearImageCache",
      },
      {
        label: l10n.t("Menu.OpenLogs"),
        description: l10n.t("Menu.OpenLogsDesc"),
        action: "openLogs",
      },
      {
        label: l10n.t("Menu.SetGameRootFolderPath"),
        description: l10n.t("Menu.SetGameRootFolderPathDesc"),
        action: "setGameRootFolderPath",
      },
      {
        label: l10n.t("Menu.ReportIssue"),
        description: l10n.t("Menu.ReportIssueDesc"),
        action: "reportIssue",
      }
    ];

    const selection = await window.showQuickPick(options);
    if (!selection) {
      return;
    }

    switch (selection.action) {
      case "openTraitsView":
        commands.executeCommand("vmoder.openTraitsView");
        break;
      case "openModifierQuerierView":
        commands.executeCommand("vmoder.openModifierQuerierView");
        break;
      case "openCharacterEditor":
        commands.executeCommand("vmoder.openCharacterEditor");
        break;
      case "clearImageCache":
        commands.executeCommand("vmoder.clearImageCache");
        break;
      case "openLogs":
        commands.executeCommand("vmoder.openLogs");
        break;
      case "setGameRootFolderPath":
        await pickGameRootFolderPath(config);
        break;
      case "reportIssue":
        env.openExternal(Uri.parse('https://github.com/textGamex/VModer/issues/new'));
        reporter.sendTelemetryEvent("reportIssue");
        break;
    }
  });

  return openMenu;
}

export function deactivate(): Thenable<void> | undefined {
  if (!client) {
    return undefined;
  }
  return client.stop();
}
