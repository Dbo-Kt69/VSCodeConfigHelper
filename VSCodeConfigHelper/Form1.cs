﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Net;
using System.Diagnostics;
using System.Security.Principal;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace VSCodeConfigHelper
{

    public partial class Form1 : Form
    {

        #region Add Shield Icon

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(HandleRef hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        /// <summary>
        ///     Enables the elevated shield icon on the given button control
        /// </summary>
        /// <param name="ThisButton">
        ///     Button control to enable the elevated shield icon on.
        /// </param>
        private void EnableElevateIcon_BCM_SETSHIELD(Button ThisButton)
        {
            // Input validation, validate that ThisControl is not null
            if (ThisButton == null) return;

            // Define BCM_SETSHIELD locally, declared originally in Commctrl.h
            uint BCM_SETSHIELD = 0x0000160C;

            // Set button style to the system style
            ThisButton.FlatStyle = FlatStyle.System;

            // Send the BCM_SETSHIELD message to the button control
            SendMessage(new HandleRef(ThisButton, ThisButton.Handle), BCM_SETSHIELD, new IntPtr(0), new IntPtr(1));
        }
        #endregion

        public Form1()
        {
            InitializeComponent();
        }

        string workspacePath = string.Empty;
        string minGWPath = string.Empty;
        bool isMinGWOk = false;
        bool isWorkspaceOk = false;
        bool isSuccess = false;
        JArray args = new JArray {
            "-g",
            "-std=c++17",
            "\"${file}\"",
            "-o",
            "\"${fileDirname}\\${fileBasenameNoExtension}.exe\""
        };
        static readonly string helpText =
            "========================================" + Environment.NewLine +
            "什么是 MinGW-w64 ？" + Environment.NewLine +
            "----------------------------------------" + Environment.NewLine +
            "MinGW (Minimalist GNU for Windows)，是一个适用于" +
            "Windows 应用程序的极简开发环境， 提供了一个完整的" +
            "开源编程工具集，Mingw-w64 则是 MinGW 的“升级版” " +
            "，提供了对 64 位计算机的支持。" + Environment.NewLine +
             Environment.NewLine + Environment.NewLine +
            "========================================" + Environment.NewLine +
            "下载下来的 MinGW-w64 文件打不开，怎么办？" + Environment.NewLine +
            "----------------------------------------" + Environment.NewLine +
            "您刚刚所下载的文件是 7-Zip 格式，一种效率较高的压" +
            "缩文件。您可以通过任何主流的解压缩工具（如 WinRAR、" +
            "Bandizip 等）解压，也可以使用专门的 7-Zip 工具（ht" +
            "tps://www.7-zip.org/）解压。" + Environment.NewLine +
            Environment.NewLine + Environment.NewLine +
            "========================================" + Environment.NewLine +
            "设置环境变量是什么意思？" + Environment.NewLine +
            "----------------------------------------" + Environment.NewLine +
            "环境变量是指在操作系统中用来指定操作系统运行环境" +
            "的一些参数。这里的设置是将 MinGW 相关程序添加到 " +
            "Path 这一环境变量当中，允许用户可以轻松地键入 `g" +
            "++` 等命令直接编译。" + Environment.NewLine +
            Environment.NewLine + Environment.NewLine +
            "========================================" + Environment.NewLine +
            "“安装插件”是在做什么？" + Environment.NewLine +
            "----------------------------------------" + Environment.NewLine +
            "VS Code 本身仅仅是一个文本编辑器，正是由于它强大的" +
            "插件生态，才能让它实现程序的编译、运行和调试。这里" +
            "安装的插件是微软官方制作的 C/C++ 插件，提供了简洁易" +
            "用的调试和 IntelliSense 智能提示功能。" + Environment.NewLine +
            Environment.NewLine + Environment.NewLine +
            "========================================" + Environment.NewLine +
            "为什么要选择工作文件夹？“一键配置”都做了什么？" + Environment.NewLine +
            "----------------------------------------" + Environment.NewLine +
            " VS Code 的核心理念和 Visual Studio 类似也是基于" +
            "“项目”这一基本单位的。在 VS Code 中，项目的表现形" +
            "式就是“工作区”（Workspace）。您的一切编译、运行配" +
            "置都只适用于工作区内部，这样您可以针对不同的语言、" +
            "不同的用途进行个性化的配置。" + Environment.NewLine +
            "当 VS Code 打开工作区文件夹时，会读取 `.vscode` 子" +
            "文件夹内部的数个 JSON 文件作为配置信息。这些 JSON " +
            "文件将通过固定的格式指示 VS Code 如何调用编译器，" +
            "如何调试，并提供运行路径等必要的信息。本工具所做的就" +
            "是通过您输入的 MinGW 路径自动配置好上述 JSON 文件。" +
            Environment.NewLine + Environment.NewLine +
            "========================================" + Environment.NewLine +
            "为什么工作文件夹不支持中文？" + Environment.NewLine +
            "----------------------------------------" + Environment.NewLine +
            "由于 MinGW 中 gdb 调试器并不支持 Unicode (UTF-16) 编码" +
            "的路径参数，详情可见 https://github.com/Microsoft/vscode-cpptools/issues/1998 " +
            "的讨论。对此我感到十分抱歉，还请您尝试其它命名，谢谢。" +
            Environment.NewLine + Environment.NewLine +
            "========================================" + Environment.NewLine +
            "如果您在配置成功后的编译、调试环节发生问题，请您浏览 " +
            "https://github.com/Guyutongxue/VSCodeConfigHelper/blob/master/TroubleShooting.md"+
            " 获取帮助。如果您还有其它方面的问题，欢迎通过下方的邮件地址" +
            "联系开发者谷雨同学。"
            ;
        readonly string testCode = @"// VS Code C++ 测试代码 ""Hello World""
// 由 VSCodeConfigHelper 生成

// 您可以在当前的文件夹（您第六步输入的文件夹）下编写代码。

// 按下 F5（部分设备上可能是 Fn + F5）编译调试。
// 按下 Ctrl + Shift + B 编译，但不运行。
// 按下 Ctrl + F5（部分设备上可能是 Ctrl + Fn + F5）编译运行，但不调试。

#include <iostream>

/**
 * 程序执行的入口点。
 */
int main() {
    // 在标准输出中打印 ""Hello, world!""
    std::cout << ""Hello, world!"" << std::endl;
    return 0;
}

// 此文件编译运行将输出 ""Hello, world!""。
// 您将在下方弹出的终端（Terminal）窗口中看到这一行字。

// 如果遇到了问题，请您浏览
// https://github.com/Guyutongxue/VSCodeConfigHelper/blob/master/TroubleShooting.md 
// 获取帮助。如果问题未能得到解决，请联系开发者。";

        public static bool IsRunningOn64Bit { get { return IntPtr.Size == 8; } }

        private static bool IsAdministrator
        {
            get
            {

                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (IsAdministrator)
            {
                labelAuth.Width = 409;
                labelAuth.Text = "当前权限：系统管理员" + Environment.NewLine;
                labelAuth.Text += "您在此工具进行的操作（包括安装、设置环境变量和启动等）" +
                    "将适用于所有用户，请谨慎操作。" + Environment.NewLine;
                labelAuth.Text += "若要使用普通用户权限，请重新使用非管理员权限运行此程序。";
                buttonAuth.Visible = false;

                Text = "管理员: VS Code C++配置工具";

            }
            else
            {
                labelAuth.Text = "当前权限：普通用户" + Environment.NewLine;
                labelAuth.Text += "您在此工具进行的操作（包括安装、设置环境变量和启动等）" +
                    "将仅适用于此账户。" + Environment.NewLine;
                labelAuth.Text += "若要使用系统管理员权限，请点击右侧按钮。";
                EnableElevateIcon_BCM_SETSHIELD(buttonAuth);
            }

            textBoxHelp.Text = helpText;

            labelAuthor.Text = $"v{Application.ProductVersion} 谷雨同学制作 guyutongxue@163.com";

            string specify = IsRunningOn64Bit ? "64" : "32";
            labelMinGWPathHint.Text = $"您解压后可以得到一个 mingw{specify} 文件夹。这里面包含着重要的编译必需文件，建议您将它移动到妥善的位置，如 C 盘根目录下。将它的路径输入在下面：";

            // 北大网盘有效期截止至此
            if (DateTime.Now.Date < new DateTime(2024, 10, 1)) radioButtonPKU.Select();
            else
            {
                radioButtonPKU.Enabled = false;
                radioButtonOffical.Select();
            }

            ShowArgs();

            if (File.Exists("VSCHcache.txt"))
            {
                StreamReader sr = new StreamReader("VSCHcache.txt");
                textBoxMinGWPath.Text = sr.ReadLine();
                textBoxWorkspacePath.Text = sr.ReadLine();
                sr.Close();
            }
        }

        private void ButtonViewMinGW_Click(object sender, EventArgs e)
        {
            DialogResult result = folderBrowserDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                textBoxMinGWPath.Text = folderBrowserDialog1.SelectedPath;
            }

        }

        private void TextBoxMinGWPath_TextChanged(object sender, EventArgs e)
        {
            isMinGWOk = false;
            minGWPath = textBoxMinGWPath.Text;
            if (!string.IsNullOrWhiteSpace(minGWPath))
            {
                if (Directory.Exists(minGWPath) && File.Exists(minGWPath + "\\bin\\g++.exe"))
                {
                    labelMinGWState.ForeColor = Color.Green;
                    labelMinGWState.Text = "检测到编译器：";
                    string version = GetGxxVersion(minGWPath + "\\bin\\g++.exe");
                    labelMinGWState.Text += '\n' + version;
                    // prevent duplicate
                    minGWPath = minGWPath.ToLower();
                    isMinGWOk = true;
                }
                else
                {
                    labelMinGWState.ForeColor = Color.Red;
                    labelMinGWState.Text = "未检测到编译器，请重试。";

                }
            }

        }

        private void ButtonSetEnv_Click(object sender, EventArgs e)
        {
            if (!isMinGWOk)
            {
                labelPathState.ForeColor = Color.Red;
                labelPathState.Text = "MinGW 路径尚未配置完成。";
                return;
            }
            // If admin, set PATH to machine; else set PATH to user.
            EnvironmentVariableTarget current = IsAdministrator ? EnvironmentVariableTarget.Machine : EnvironmentVariableTarget.User;
            string path = Environment.GetEnvironmentVariable("Path", current);
            if (path.Contains(minGWPath + "\\bin"))
            {
                labelPathState.ForeColor = Color.Green;
                labelPathState.Text = "环境变量已设置。";
                return;
            }
            Environment.SetEnvironmentVariable("Path", path + ";" + minGWPath + "\\bin", current);
            // Check
            path = Environment.GetEnvironmentVariable("Path", current);
            if (path.Contains(minGWPath + "\\bin"))
            {
                labelPathState.ForeColor = Color.Green;
                labelPathState.Text = "设置环境变量成功。";
            }
            else
            {
                labelPathState.ForeColor = Color.Red;
                labelPathState.Text = "设置环境变量失败，请重试。";
            }
        }

        private static string GetGxxVersion(string path)
        {

            string result = string.Empty;
            using (Process proc = new Process())
            {
                proc.StartInfo.CreateNoWindow = true;
                proc.StartInfo.FileName = path;
                proc.StartInfo.Arguments = "--version";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.RedirectStandardInput = true;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.Start();
                result = proc.StandardOutput.ReadLine();
                proc.WaitForExit();
                proc.Close();
            }
            return result;
        }



        private void ButtonExtension_Click(object sender, EventArgs e)
        {
            string cppLink = @"https://marketplace.visualstudio.com/items?itemName=ms-vscode.cpptools";
            Process.Start(cppLink);
        }

        private void LinkLabelMinGW_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string mingwLink;
            if (IsRunningOn64Bit)
            {
                if (radioButtonPKU.Checked)
                    mingwLink = @"https://disk.pku.edu.cn:443/link/B897756E8392A02AD20F56C6A17E0655";
                else
                    mingwLink = @"https://sourceforge.net/projects/mingw-w64/files/Toolchains%20targetting%20Win64/Personal%20Builds/mingw-builds/8.1.0/threads-win32/seh/x86_64-8.1.0-release-win32-seh-rt_v6-rev0.7z";
            }
            else
            {
                if (radioButtonPKU.Checked)
                    mingwLink = @"https://disk.pku.edu.cn:443/link/E9E6D208F9AEC29D7D77BA2A923A6400";
                else
                    mingwLink = @"https://sourceforge.net/projects/mingw-w64/files/Toolchains%20targetting%20Win32/Personal%20Builds/mingw-builds/8.1.0/threads-win32/dwarf/i686-8.1.0-release-win32-dwarf-rt_v6-rev0.7z";
            }
            Process.Start(mingwLink);
        }


        private void LinkLabelVSCode_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {

                string adminSpec = IsAdministrator ? "" : "-user";
                string bitSpec = IsRunningOn64Bit ? "-x64" : "";
                Process.Start("https://update.code.visualstudio.com/latest/win32" + bitSpec + adminSpec + "/stable");
            }
            catch (Exception)
            {
                // Shouldn't be executed
                MessageBox.Show("无法获得直接下载地址，请手动点击下载" + (IsAdministrator ? " System 版本安装包" : "") + "。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Process.Start("https://code.visualstudio.com/Download");
            }
            // Hint image (Open by browser)
            Process.Start("https://s2.ax1x.com/2020/01/18/1pRERI.png");
        }

        private void ButtonViewWorkspace_Click(object sender, EventArgs e)
        {
            DialogResult result = folderBrowserDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                textBoxWorkspacePath.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        private JObject GetLaunchJson()
        {
            JObject command = new JObject
            {
                {"description", "Enable pretty-printing for gdb"},
                {"text", "-enable-pretty-printing"},
                {"ignoreFailures", true}
            };
            JObject config = new JObject
            {
                {"name", "g++.exe build and debug active file"},
                {"type", "cppdbg"},
                {"request", "launch"},
                {"program", "${fileDirname}\\${fileBasenameNoExtension}.exe"},
                {"args", new JArray()},
                {"stopAtEntry", false},
                {"cwd", "${workspaceFolder}"},
                {"environment", new JArray()},
                {"externalConsole", false},
                {"MIMode", "gdb"},
                {"miDebuggerPath", minGWPath+"\\bin\\gdb.exe"},
                {"setupCommands",new JArray{command} },
                {"preLaunchTask", "g++.exe build active file" },
                {"internalConsoleOptions", "neverOpen" }
            };

            JObject launch = new JObject
            {
                { "version", "0.2.0" },
                {"configurations",new JArray{config} }
            };
            return launch;
        }

        private JObject getTasksJson()
        {
            JObject group = new JObject
            {
                {"kind", "build"},
                {"isDefault", true}
            };
            JObject presentation = new JObject
            {
                {"echo", true},
                {"reveal", "always"},
                {"focus", false},
                {"panel", "shared"},
                {"showReuseMessage", true},
                {"clear", false}
            };
            JObject problemMatcher = new JObject
            {
                { "owner", "cpp" },
                { "fileLocation", "absolute"},
                { "pattern",new JObject{
                    {"regexp", "^(.*):(\\d+):(\\d+):\\s+(error):\\s+(.*)$"},
                    {"file", 1},
                    {"line", 2},
                    {"column", 3},
                    {"severity", 4},
                    {"message", 5}
                } }
            };
            JObject tasks = new JObject
            {
                { "version","2.0.0" },
                { "tasks",new JArray
                    {
                        new JObject
                        {
                            {"type", "shell"},
                            {"label", "g++.exe build active file"},
                            {"command", "g++.exe"},
                            {"args",args},
                            {"group",group},
                            {"presentation",presentation},
                            {"problemMatcher",problemMatcher}
                        }
                    }
                },
                // https://github.com/microsoft/vscode/issues/70509
                { "options",new JObject
                    { {
                        "shell", new JObject
                        {
                            { "executable", "cmd.exe" },
                            { "args", new JArray("/c") }
                        }
                    } }
                }
            };
            return tasks;
        }

        private JObject GetSettingsJson()
        {
            return new JObject
            {{
                "C_Cpp.default.intelliSenseMode", "gcc-x64"
            }};
        }

        private string GenerateTestFile(string path)
        {
            labelConfigState.Text += "生成测试文件中...";
            string filepath = $"{path}\\helloworld.cpp";
            if (File.Exists(filepath))
            {
                for (int i = 1; ; i++)
                {
                    filepath = $"{path}\\helloworld({i}).cpp";
                    if (!File.Exists(filepath)) break;
                }
            }
            StreamWriter sw = new StreamWriter(filepath, false, Encoding.UTF8);
            sw.Write(testCode);
            sw.Flush();
            sw.Close();
            labelConfigState.Text += "成功。";
            return filepath;
        }

        private void LoadVSCode(string path, string filepath = null)
        {
            try
            {
                labelConfigState.Text += "启动 VS Code 中...";
                using (Process proc = new Process())
                {
                    // Must execute by shell to use User PATH
                    // Hide the Shell Console Window
                    proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    proc.StartInfo.UseShellExecute = true;
                    proc.StartInfo.FileName = "code";
                    if (string.IsNullOrEmpty(filepath))
                    {
                        proc.StartInfo.Arguments = $"\"{path}\"";
                    }
                    else
                    {
                        proc.StartInfo.Arguments = $"\"{path}\" -g \"{filepath}\"";
                    }
                    proc.Start();
                    proc.WaitForExit();
                    proc.Close();
                }
                labelConfigState.Text += "成功。";
            }
            catch (Exception)
            {
                MessageBox.Show("暂时无法启动 VS Code，请尝试手动启动或者重新打开本工具。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                throw new Exception("启动 VS Code 失败。");
            }
        }


        private void ButtonConfig_Click(object sender, EventArgs e)
        {
            try
            {
                isSuccess = false;
                workspacePath = textBoxWorkspacePath.Text;
                JObject launchJson = GetLaunchJson();
                JObject tasksJson = getTasksJson();
                JObject settingsJson = GetSettingsJson();
                if (!isWorkspaceOk || !isMinGWOk)
                {
                    labelConfigState.ForeColor = Color.Red;
                    labelConfigState.Text = "MinGW 路径或工作文件夹尚未配置完成。";
                    return;
                }
                if (Directory.Exists(workspacePath + "\\.vscode"))
                {
                    DialogResult result = MessageBox.Show("检测到已有配置，是否覆盖？", "提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
                    if (result == DialogResult.Cancel) return;
                    Directory.Delete(workspacePath + "\\.vscode", true);
                }

                // Kill VS Code process to apply PATH env and prevent occupy
                Process[] processList = System.Diagnostics.Process.GetProcesses();
                foreach (var process in processList)
                {
                    if (process.ProcessName.ToLower() == "code")
                    {
                        process.Kill();
                    }
                }

                Directory.CreateDirectory(workspacePath + "\\.vscode");
                File.SetAttributes(workspacePath + "\\.vscode", FileAttributes.Hidden);
                StreamWriter launchsw = new StreamWriter(workspacePath + "\\.vscode\\launch.json");
                launchsw.Write(launchJson.ToString());
                launchsw.Flush();
                launchsw.Close();
                StreamWriter taskssw = new StreamWriter(workspacePath + "\\.vscode\\tasks.json");
                taskssw.Write(tasksJson.ToString());
                taskssw.Flush();
                taskssw.Close();
                StreamWriter settingssw = new StreamWriter(workspacePath + "\\.vscode\\settings.json");
                settingssw.Write(settingsJson.ToString());
                settingssw.Flush();
                settingssw.Close();
                labelConfigState.ForeColor = Color.Green;
                labelConfigState.Text = "配置成功。";

                if (checkBoxGenTest.Checked)
                {
                    string filepath = GenerateTestFile(workspacePath);
                    if (checkBoxOpen.Checked) LoadVSCode(workspacePath, filepath);
                }
                else if (checkBoxOpen.Checked) LoadVSCode(workspacePath);
                isSuccess = true;
            }
            catch (Exception ex)
            {
                labelConfigState.ForeColor = Color.Red;
                labelConfigState.Text += "配置失败：" + ex.Message;
            }
        }



        private void LinkLabelManual_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string manualLink = @"https://github.com/Guyutongxue/VSCodeConfigHelper/blob/master/README.md";
            Process.Start(manualLink);
        }

        private void TextBoxWorkspacePath_TextChanged(object sender, EventArgs e)
        {
            workspacePath = textBoxWorkspacePath.Text;
            if (string.IsNullOrWhiteSpace(workspacePath))
            {
                isWorkspaceOk = false;
                labelWorkspaceStatus.Visible = false;
                return;
            }
            if (!Regex.IsMatch(workspacePath, "^[ -~]*$"))
            {
                isWorkspaceOk = false;
                labelWorkspaceStatus.Visible = true;
            }
            else
            {
                isWorkspaceOk = true;
                labelWorkspaceStatus.Visible = false;
            }
        }

        private void GenerateArgs()
        {
            string text = textBoxArgs.Text.Trim();
            string[] argtext = text.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            args = new JArray(argtext);
        }

        private void ShowArgs()
        {
            StringBuilder text = new StringBuilder();
            foreach (object i in args)
            {
                text.AppendLine(i.ToString());
            }
            textBoxArgs.Text = text.ToString().Trim();
        }

        private void buttonSaveArgs_Click(object sender, EventArgs e)
        {
            GenerateArgs();
            ShowArgs();
        }

        private void buttonArgDefault_Click(object sender, EventArgs e)
        {
            args = new JArray {
                "-g",
                "-std=c++17",
                "\"${file}\"",
                "-o",
                "\"${fileDirname}\\${fileBasenameNoExtension}.exe\""
            };
            ShowArgs();
        }

        private void buttonAuth_Click(object sender, EventArgs e)
        {
            try
            {
                var exeName = Process.GetCurrentProcess().MainModule.FileName;
                ProcessStartInfo startInfo = new ProcessStartInfo(exeName);
                startInfo.Verb = "runas";
                Process.Start(startInfo);
                Application.Exit();
            }
            catch (Win32Exception)
            {
                // Do nothing.
                // If user cancel the operation by UAC, Process.Start will
                // throw an exception. Just ignore it.
            }
        }

        private void radioButtonPKU_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonOffical.Checked)
            {
                linkLabelMinGW.Text = "下载地址";
            }
            else
            {
                linkLabelMinGW.Text = "下载地址...";
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!isSuccess)
            {
                StreamWriter sw = new StreamWriter("VSCHcache.txt");
                sw.WriteLine(minGWPath);
                sw.WriteLine(workspacePath);
                sw.Flush();
                sw.Close();
            }
            else
            {
                if (File.Exists("VSCHcache.txt"))
                {
                    File.Delete("VSCHcache.txt");
                }
            }
        }
    }
}
