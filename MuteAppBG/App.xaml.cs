using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using Tommy;

namespace MuteAppBG;

public partial class App
{
    private readonly List<string> _mutedApps;
    private readonly NotifyIcon _notifyIcon;
    private readonly List<string> _targetAppList;
    private uint _activeWindow;
    private bool _isExit;

    public App()
    {
        _targetAppList = new List<string>();
        RefreshConfig();

        _mutedApps = new List<string>();
        _activeWindow = 0;
        _notifyIcon = new NotifyIcon();
    }

    private void RefreshConfig()
    {
        if (!File.Exists("configuration.toml"))
        {
            using var writer = File.CreateText("configuration.toml");
            var toml = new TomlTable
            {
                ["appsToMute"] = new TomlArray
                {
                    "Game.exe"
                }
            };
            toml.WriteTo(writer);
            writer.Flush();
        }

        TomlTable? table = null;
        try
        {
            using var reader = File.OpenText("configuration.toml");
            table = TOML.Parse(reader);
        }
        catch
        {
            // ignored
        }

        if (table?["appsToMute"] is not TomlArray appsToMute) return;
        _targetAppList.Clear();
        foreach (var node in appsToMute)
            if (node is TomlString appName)
                _targetAppList.Add(appName.Value.TrimEnd(".exe"));
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        MainWindow = new MainWindow();
        MainWindow.Closing += MainWindow_Closing;

        //_notifyIcon.DoubleClick += (s, args) => ShowMainWindow();
        _notifyIcon.Icon = MuteAppBG.Properties.Resources.TrayIcon;
        _notifyIcon.Visible = true;

        CreateContextMenu();

        User32Event.ActiveWindowChanged += OnUser32EventOnActiveWindowChanged;

        User32Event.Start();
    }

    private void OnUser32EventOnActiveWindowChanged(object? _, uint pidSent)
    {
        var nextPid = User32Event.ActiveWindowQueue.Dequeue();
        var prevActiveWindow = _activeWindow;
        _activeWindow = nextPid;

        if (prevActiveWindow == _activeWindow) return;

        try
        {
            var pid = prevActiveWindow;
            using var p = Process.GetProcessById(Convert.ToInt32(pid));
            var fn = p.ProcessName;
            if (pid > 0 && !string.IsNullOrEmpty(fn) && _targetAppList.Contains(fn))
            {
                var vcPre = new VolumeMixer(fn);
                if (!vcPre.Active()) return;
                if (!vcPre.Mute)
                {
                    if (!_mutedApps.Contains(fn)) _mutedApps.Add(fn);
                    vcPre.Mute = true;
                }
            }
        }
        catch
        {
            // ignored
        }

        try
        {
            var pid = _activeWindow;
            using var p = Process.GetProcessById(Convert.ToInt32(pid));
            var fn = p.ProcessName;
            if (pid <= 0 || string.IsNullOrEmpty(fn)) return;
            if (!_targetAppList.Contains(fn)) return;
            if (!_mutedApps.Contains(fn)) return;
            var vc = new VolumeMixer(fn);
            if (!vc.Active()) return;
            vc.Mute = false;
            _mutedApps.Remove(fn);
        }
        catch
        {
            // ignored
        }
    }

    private void CreateContextMenu()
    {
        _notifyIcon.ContextMenuStrip =
            new ContextMenuStrip();
        _notifyIcon.ContextMenuStrip.Items.Add("Reload Config").Click += (_, _) => RefreshConfig();
        _notifyIcon.ContextMenuStrip.Items.Add("Exit").Click += (_, _) => ExitApplication();
    }

    private void ExitApplication()
    {
        _isExit = true;
        MainWindow?.Close();
        _notifyIcon.Dispose();
    }

    private void ShowMainWindow()
    {
        if (MainWindow is { IsVisible: true })
        {
            if (MainWindow.WindowState == WindowState.Minimized) MainWindow.WindowState = WindowState.Normal;
            MainWindow.Activate();
        }
        else
        {
            MainWindow?.Show();
        }
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        if (_isExit) return;
        e.Cancel = true;
        MainWindow?.Hide();
    }
}