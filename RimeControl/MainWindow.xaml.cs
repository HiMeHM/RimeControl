﻿using Microsoft.Win32;
using RimeControl.Entitys;
using RimeControl.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using Xceed.Wpf.Toolkit;
using CheckBox = System.Windows.Controls.CheckBox;
using MessageBox = System.Windows.MessageBox;
using TextBox = System.Windows.Controls.TextBox;

namespace RimeControl
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        /***********************全局变量**************************/
        #region 全局变量
        private string _strUserFolderPath = "";      //小狼毫输入法的用户目录路径
        private string _strRootFolderPath = "";      //小狼毫输入法root目录路径
        //public readonly string StrProjectRootPath = AppDomain.CurrentDomain.BaseDirectory;      //程序运行目录
        //获取用户应用程序目录中的Rime目录  C:\Users\用户名\AppData\Roaming\Rime
        private readonly string _userRoamingFolderRime = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Rime";

        private string _strUserDefaultCustomPath = "\\default.custom.yaml";     //default.custom.yaml的文件路径 
        private string _strUserWeaselCustomPathPath = "\\weasel.custom.yaml";     //weasel.custom.yaml的文件  路径

        private string _strUserDefaultPath = "\\build\\default.yaml";     //default.yaml的文件路径、
        private string _strUserWeaselPath = "\\build\\weasel.yaml";     //weasel.yaml的文件路径

        private string _strRootWeaselPath = "\\data\\weasel.yaml";// 程序安装目录 data目录下 weasel.yaml的文件路径，为了判断哪些Scheme是预设的

        private Yaml _yamlUserDefaultFile;                 //default.yaml文件
        private Yaml _yamlUserDefaultCustomFile;         //default.custom.yaml文件
        private Yaml _yamlUserWeaselFile;                  //weasel.yaml文件
        private Yaml _yamlUserWeaselCustomFile;         //weasel.custom.yaml文件
        private Yaml _yamlRootWeaselFile;                  //程序安装目录 data目录下 weasel.yaml文件


        //中英文切换快捷键 数字与值得相互转换
        readonly Dictionary<int, string> _dirCobCtrlLf = new Dictionary<int, string>();
        readonly Dictionary<string, int> _dirCobCtrl = new Dictionary<string, int>();

        //方案选单快捷键 

        //皮肤
        private readonly ObservableCollection<Scheme> _listScheme = new ObservableCollection<Scheme>(); //list的数据源
        private string _usingSchemeId; //正在使用的皮肤Id
        private int _newSchemeIndex;//添加皮肤时使用标识序号
        private bool _schemeInfoLoading = true;//标识选择Scheme后加载绑定Scheme信息的过程，避免触发信息改变的事件
        //方案
        private ObservableCollection<Schema> _listSchemaList = new ObservableCollection<Schema>();   //方案列表
        
        //备份与还原
        private ObservableCollection<BackAndRestoreItems> _dgBackAndRestoreItem;//备份DataGrid数据源

        private readonly string _strBackupsFolder = AppDomain.CurrentDomain.BaseDirectory + "Backups\\";

        private bool _isBackup = true;//表示加载页面是备份还是还原
        private int _backupRestoreResult;//备份or还原方法的结果

        //GitHub上的最新版本号
        private string _newVersion;
        #endregion


        #region 路径拼接，文件读取，页面准备

        /// <summary>
        /// 读取 Rime注册表信息
        ///
        /// 通过注册表获取小狼毫输入法的用户目录 并判断是否存在 不存在手动指定目录
        /// </summary>
        public void ReadRimeRegistry()
        {
            bool bolTrues = true;   //标记文件是否找到
            bool bolClose = false;  //标记是否终止程序

            #region 查找用户配置目录


            //==开始读取注册表信息
            var myRegistryKey = Registry.CurrentUser;
            //读取Software\\Rime\\Weasel  win10下rime注册表是这个路径
            RegistryKey myReg = myRegistryKey.OpenSubKey("Software\\Rime\\Weasel");
            try
            {
                if (myReg != null)
                {
                    _strUserFolderPath = myReg.GetValue("RimeUserDir").ToString();
                    myReg.Close();//关闭注册表阅读器
                    //检查文件是否存在
                    bolTrues = File.Exists(_strUserFolderPath + "\\build\\default.yaml");
                    if (!bolTrues)
                    {
                        _strUserFolderPath = "";
                    }
                }
            }
            catch (Exception ex)
            {
                _strUserFolderPath = "";
                Console.WriteLine(ex);
            }

            //判断_strUserFolderPath是否为null或空字符
            //如果为空，尝试读取 系统用户名称AppData\\Rime目录
            if (string.IsNullOrEmpty(_strUserFolderPath))
            {
                //获取系统用户目录
                _strUserFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Rime";
                bolTrues = File.Exists(_strUserFolderPath + "\\build\\default.yaml");
                if (!bolTrues)
                {
                    _strUserFolderPath = "";
                }
            }

            //文件还是未找到，_strUserFolderPath为null或空字符
            //还未找到配置目录，手动指定
            if (!bolTrues && string.IsNullOrEmpty(_strUserFolderPath))
            {
                do
                {
                    if (MessageBox.Show("小狼毫用户配置目录未找到，请手动指定：", "提示",
                            MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.OK)
                    {
                        FolderBrowserDialog myFolderBrowserDialog = new FolderBrowserDialog();
                        if (myFolderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        {
                            if (myFolderBrowserDialog.SelectedPath != string.Empty)
                            {
                                _strUserFolderPath = myFolderBrowserDialog.SelectedPath;
                            }
                            bolTrues = File.Exists(_strUserFolderPath + "\\build\\default.yaml");
                        }
                    }
                    else
                    {
                        bolClose = true;
                        break;
                    }

                } while (!bolTrues);
            }

            if (bolClose)
            {
                Environment.Exit(0);
            }
            bolTrues = true;

            #endregion

            #region 小狼毫安装目录
            //读取小狼毫安装目录 64位系统，
            myRegistryKey = Registry.LocalMachine;
            myReg = myRegistryKey.OpenSubKey("SOFTWARE\\Wow6432Node\\Rime\\Weasel");
            try
            {
                if (myReg != null)
                {
                    _strRootFolderPath = myReg.GetValue("WeaselRoot").ToString();
                    myReg.Close();
                }
            }
            catch (Exception ex)
            {
                _strRootFolderPath = null;
                Console.WriteLine(ex);
            }

            // 32系统位待测
            if (string.IsNullOrEmpty(_strRootFolderPath))
            {
                myReg = myRegistryKey.OpenSubKey("SOFTWARE\\Rime\\Weasel");
                try
                {
                    if (myReg != null)
                    {
                        _strRootFolderPath = myReg.GetValue("WeaselRoot").ToString();
                        myReg.Close();
                    }
                }
                catch (Exception)
                {

                    _strRootFolderPath = null;
                }
            }

            //未找到，手动指定狼毫安装目录
            if (string.IsNullOrEmpty(_strRootFolderPath))
            {
                do
                {
                    if (MessageBox.Show("小狼毫安装目录未找到，请手动指定(到 带版本号的目录里weasel-0.13.0)：", "提示", MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.OK)
                    {
                        FolderBrowserDialog myFolderBrowserDialog = new FolderBrowserDialog();
                        if (myFolderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        {
                            if (myFolderBrowserDialog.SelectedPath != string.Empty)
                            {
                                _strRootFolderPath = myFolderBrowserDialog.SelectedPath;
                            }

                            bolTrues = File.Exists(_strRootFolderPath + "\\WeaselDeployer.exe");
                        }
                    }
                    else
                    {
                        bolClose = true;
                        break;
                    }
                } while (!bolTrues);
            }
            if (bolClose)
            {
                Environment.Exit(0);
            }

            #endregion


            //显示路径信息
            string strBasePathInfo = "安装目录:" + _strRootFolderPath + "    配置目录;" + _strUserFolderPath;
            TbBasePath.Text = strBasePathInfo;


            //拼接路径
            _strUserDefaultPath = _strUserFolderPath + _strUserDefaultPath;
            _strUserDefaultCustomPath = _strUserFolderPath + _strUserDefaultCustomPath;

            _strUserWeaselPath = _strUserFolderPath + _strUserWeaselPath;
            _strUserWeaselCustomPathPath = _strUserFolderPath + _strUserWeaselCustomPathPath;

            _strRootWeaselPath = _strRootFolderPath + _strRootWeaselPath;
        }

        /// <summary>
        /// 常规页面绑定ComboBox
        /// </summary>
        public void BindingComboBox()
        {
            //==字体大小 下拉框
            Dictionary<string, int> dirCobFontSize = new Dictionary<string, int>();
            for (int j = 0; j < 41; j++)
            {
                dirCobFontSize.Add((10 + j).ToString(), (10 + j));
            }
            //绑定下拉框
            CobFontSize.ItemsSource = dirCobFontSize;
            CobFontSize.SelectedValuePath = "Value";
            CobFontSize.DisplayMemberPath = "Key";
            CobFontSize.SelectedValue = 16;

            //==候选数
            Dictionary<string, int> dirCobChooseNum = new Dictionary<string, int>();
            for (int i = 0; i < 10; i++)
            {
                dirCobChooseNum.Add((i + 1).ToString(), i + 1);
            }
            //绑定下拉框
            CobPageSize.ItemsSource = dirCobChooseNum;
            CobPageSize.SelectedValuePath = "Value";
            CobPageSize.DisplayMemberPath = "Key";
            CobPageSize.SelectedValue = 5;

            //==中英文切换 信息
            Dictionary<string, int> dirCobCtrlL = new Dictionary<string, int>();
            //
            _dirCobCtrlLf.Add(0, "noop");
            _dirCobCtrl.Add("noop", 0);
            dirCobCtrlL.Add("屏蔽该切换键", 0);

            _dirCobCtrlLf.Add(1, "commit_code");
            _dirCobCtrl.Add("commit_code", 1);
            dirCobCtrlL.Add("编码字符上屏并切换至西文", 1);

            _dirCobCtrlLf.Add(2, "commit_text");
            _dirCobCtrl.Add("commit_text", 2);
            dirCobCtrlL.Add("候选文字上屏并切换至西文", 2);

            _dirCobCtrlLf.Add(3, "inline_ascii");
            _dirCobCtrl.Add("inline_ascii", 3);
            dirCobCtrlL.Add("回车上屏后自动复位到中文", 3);

            _dirCobCtrlLf.Add(4, "clear");
            _dirCobCtrl.Add("clear", 4);
            dirCobCtrlL.Add("清除编码并切换至西文", 4);
            //=绑定下拉框
            //Ctrl 左
            CobCtrlL.ItemsSource = dirCobCtrlL;
            CobCtrlL.SelectedValuePath = "Value";
            CobCtrlL.DisplayMemberPath = "Key";
            CobCtrlL.SelectedValue = 3;
            //Ctrl 右
            CobCtrlR.ItemsSource = dirCobCtrlL;
            CobCtrlR.SelectedValuePath = "Value";
            CobCtrlR.DisplayMemberPath = "Key";
            CobCtrlR.SelectedValue = 3;
            //Shift 左
            CobShiftL.ItemsSource = dirCobCtrlL;
            CobShiftL.SelectedValuePath = "Value";
            CobShiftL.DisplayMemberPath = "Key";
            CobShiftL.SelectedValue = 3;
            //Shift 右
            CobShiftR.ItemsSource = dirCobCtrlL;
            CobShiftR.SelectedValuePath = "Value";
            CobShiftR.DisplayMemberPath = "Key";
            CobShiftR.SelectedValue = 3;
            //CopsLock 大写锁定
            CobCopsLock.ItemsSource = dirCobCtrlL;
            CobCopsLock.SelectedValuePath = "Value";
            CobCopsLock.DisplayMemberPath = "Key";
            CobCopsLock.SelectedValue = 3;
            //
            CobEisutoggle.ItemsSource = dirCobCtrlL;
            CobEisutoggle.SelectedValuePath = "Value";
            CobEisutoggle.DisplayMemberPath = "Key";
            CobEisutoggle.SelectedValue = 3;
        }


        /// <summary>
        /// 配置文件载入
        /// </summary>
        public void ProfileLoad()
        {
            //default.yaml文件
            _yamlUserDefaultFile = new Yaml(_strUserDefaultPath);
            //default.custom.yaml文件
            _yamlUserDefaultCustomFile = new Yaml(_strUserDefaultCustomPath);
            //weasel.yaml文件
            _yamlUserWeaselFile = new Yaml(_strUserWeaselPath);
            //weasel.custom.yaml文件
            _yamlUserWeaselCustomFile = new Yaml(_strUserWeaselCustomPathPath);
            //程序安装目录 data目录下 weasel.yaml文件
            _yamlRootWeaselFile = new Yaml(_strRootWeaselPath);
        }

        /// <summary>
        /// 控件初始化
        /// </summary>
        private void SetControl()
        {
            //===初始化 ColorPicker
            ObservableCollection<ColorItem> colors = new ObservableCollection<ColorItem>
            {
                new ColorItem(Color.FromRgb(0, 0, 0), "Black"),
                new ColorItem(Color.FromRgb(255, 255, 255), "White"),
                new ColorItem(Color.FromRgb(255, 0, 0), "Red"),
                new ColorItem(Color.FromRgb(0, 255, 0), "Lime"),
                new ColorItem(Color.FromRgb(0, 0, 255), "Blue")
            };
            ColorPicker[] colorPickers = new ColorPicker[] { ColorBack, ColorBorder, ColorText, ColorHilitedText, ColorHilitedBack, ColorHilitedCandidateText, ColorHilitedCandidateBack, ColorCandidateText, ColorCommentText };
            foreach (ColorPicker picker in colorPickers)
            {
                picker.AdvancedButtonHeader = "高级";
                picker.StandardButtonHeader = "标准";
                picker.AvailableColorsHeader = "可用颜色";
                picker.StandardColorsHeader = "标准颜色";
                picker.StandardColors = colors;
            }
        }
        #endregion

        #region 读取数据并绑定


        /// <summary>
        /// 设置数据读取绑定
        /// </summary>
        private void SettingLoad()
        {
            GeneralSetting();//常规设置的载入

            LoadSchemes();//载入皮肤数据

            //载入方案信息
            //LoadSchemas();
            BackgroundWorker bwLoadSchemas = new BackgroundWorker();
            bwLoadSchemas.DoWork += LoadSchemas;//执行的任务
            bwLoadSchemas.RunWorkerCompleted += LoadSchemasCompleted;//任务执行完成后的回调
            bwLoadSchemas.RunWorkerAsync();//开始执行后台任务

            LoadBackInfo();//载入备份信息
        }
        /// <summary>
        /// 常规设置的载入
        /// </summary>
        private void GeneralSetting()
        {
            #region 字体--weasel.custom.yaml和weasel.yaml
            //读取
            string strFont = Tools.GetYamlValue(_yamlUserWeaselFile, "style.font_face");
            //非空
            if (!string.IsNullOrEmpty(strFont))
            {
                strFont = Tools.RemoveFirstLastQuotationMarks(strFont);
                TextFonts.Text = strFont;
                TextFonts.FontFamily = new FontFamily(strFont);
            }
            #endregion

            #region 字号--weasel.custom.yaml和weasel.yaml

            string strFontSize = Tools.GetYamlValue(_yamlUserWeaselFile, "style.font_point");
            //是否为数字
            if (Tools.IsInt(strFontSize))
            {
                CobFontSize.SelectedValue = Convert.ToInt32(strFontSize);
            }
            #endregion

            #region 横竖候选框--weasel.custom.yaml和weasel.yaml

            string strHorizontal = Tools.GetYamlValue(_yamlUserWeaselFile, "style.horizontal");
            //判断
            if (Convert.ToBoolean(strHorizontal))
            {
                RdbHorizontal.IsChecked = true;
            }
            else
            {
                RdbVertical.IsChecked = true;
            }
            #endregion

            #region 托盘图标--weasel.custom.yaml

            string strDisplayTrayIcon = Tools.GetYamlValue(_yamlUserWeaselFile, "style.display_tray_icon");
            CkbDisplayTrayIcon.IsChecked = Convert.ToBoolean(strDisplayTrayIcon);

            #endregion

            #region 内嵌编码--weasel.custom.yaml

            string inlinePreedit = Tools.GetYamlValue(_yamlUserWeaselFile, "style.inline_preedit");
            CkbInlinePreedit.IsChecked = Convert.ToBoolean(inlinePreedit);

            #endregion

            #region 全屏--weasel.custom.yaml
            string fullscreen = Tools.GetYamlValue(_yamlUserWeaselFile, "style.fullscreen");
            CkbFullScreen.IsChecked = Convert.ToBoolean(fullscreen);

            #endregion

            #region 候选数--default.custom.yaml

            string pageSize = Tools.GetYamlValue(_yamlUserDefaultFile, "menu.page_size");
            CobPageSize.SelectedValue = Convert.ToInt32(pageSize);

            #endregion


            #region good_old_caps_lock--default.custom.yaml

            string goodOldCapsLock = Tools.GetYamlValue(_yamlUserDefaultFile, "ascii_composer.good_old_caps_lock");
            CkbGoodOldCapsLock.IsChecked = Convert.ToBoolean(goodOldCapsLock);

            #endregion

            #region switch_key  中英文切换快捷键
            #region switch_key>Control_L--default.custom.yaml

            string controlL = Tools.GetYamlValue(_yamlUserDefaultFile, "ascii_composer.switch_key.Control_L");
            CobCtrlL.SelectedValue = GetSwitchKeySelectedValue(controlL);//避免不存在时程序无法运行

            #endregion

            #region switch_key>Control_R--default.custom.yaml

            string controlR = Tools.GetYamlValue(_yamlUserDefaultFile, "ascii_composer.switch_key.Control_R");
            CobCtrlR.SelectedValue = GetSwitchKeySelectedValue(controlR);//避免不存在时程序无法运行

            #endregion

            #region switch_key>Shift_L--default.custom.yaml

            string shiftL = Tools.GetYamlValue(_yamlUserDefaultFile, "ascii_composer.switch_key.Shift_L");
            CobShiftL.SelectedValue = GetSwitchKeySelectedValue(shiftL);//避免不存在时程序无法运行

            #endregion

            #region switch_key>Shift_R--default.custom.yaml

            string shiftR = Tools.GetYamlValue(_yamlUserDefaultFile, "ascii_composer.switch_key.Shift_R");
            CobShiftR.SelectedValue = GetSwitchKeySelectedValue(shiftR);//避免不存在时程序无法运行

            #endregion

            #region switch_key>Caps_Lock--default.custom.yaml

            string capsLock = Tools.GetYamlValue(_yamlUserDefaultFile, "ascii_composer.switch_key.Caps_Lock");
            CobCopsLock.SelectedValue = GetSwitchKeySelectedValue(capsLock);//避免不存在时程序无法运行

            #endregion

            #region switch_key>Eisu_toggle--default.custom.yaml

            string eisuToggle = Tools.GetYamlValue(_yamlUserDefaultFile, "ascii_composer.switch_key.Eisu_toggle");
            CobEisutoggle.SelectedValue = GetSwitchKeySelectedValue(eisuToggle);//避免不存在时程序无法运行

            #endregion
            #endregion

            #region 方案选单

            string caption = Tools.GetYamlValue(_yamlUserDefaultFile, "switcher.caption");
            caption = Tools.RemoveFirstLastQuotationMarks(caption);
            TxtFangAnName.Text = caption;

            #endregion

            #region 快捷键选项
            //获取switcher.hotkeys 节点
            Yaml.Node hotkeysNode = _yamlUserDefaultFile.FindNodeByKey("switcher.hotkeys");
            if (hotkeysNode != null)
            {
                //查询switcher.hotkeys 节点的子节点就是快捷键信息
                List<Yaml.Node> oldHotKeys = _yamlUserDefaultFile.NodeList.Where(n => n.Parent == hotkeysNode).ToList();
                CkbHotKey1.IsChecked = false;
                CkbHotKey2.IsChecked = false;
                CkbHotKey3.IsChecked = false;
                CkbHotKey4.IsChecked = false;
                TxtHotKey1.Text = "";
                TxtHotKey2.Text = "";
                TxtHotKey3.Text = "";
                TxtHotKey4.Text = "";

                for (int i = 0; i < oldHotKeys.Count; i++)
                {
                    //- "Control+grave"
                    string strKey = oldHotKeys[i].Name.Replace("- ", "").Trim();//去掉“- ”，并去空格
                    strKey = Tools.RemoveFirstLastQuotationMarks(strKey);// 去掉""
                    switch (i)
                    {
                        case 0:
                            CkbHotKey1.IsChecked = true;
                            TxtHotKey1.Text = strKey;
                            break;
                        case 1:
                            CkbHotKey2.IsChecked = true;
                            TxtHotKey2.Text = strKey;
                            break;
                        case 2:
                            CkbHotKey3.IsChecked = true;
                            TxtHotKey3.Text = strKey;
                            break;
                        case 3:
                            CkbHotKey4.IsChecked = true;
                            TxtHotKey4.Text = strKey;
                            break;
                    }
                }
            }

            #endregion

        }

        /// <summary>
        /// 在载入Switch key 快捷键设置时处理未知写法（容错）
        /// 未知写法：未在小狼毫快捷键方式定义里面的
        /// 2019年4月24日 18:20
        /// 感谢 github 的 thisistianqi 反馈问题
        /// </summary>
        /// <param name="strKey"></param>
        /// <returns></returns>
        private int GetSwitchKeySelectedValue(string strKey)
        {
            int intValue = 0;
            strKey = string.IsNullOrEmpty(strKey) ? "noop" : strKey;//避免不存在时程序无法运行
            if (_dirCobCtrl.ContainsKey(strKey))
            {
                intValue = _dirCobCtrl[strKey];
            }
            return intValue;
        }



        /// <summary>
        /// Scheme载入
        /// </summary>
        private void LoadSchemes()
        {
            //加载前清空_listScheme
            _listScheme.Clear();
            //读取weasel.yaml 文件获取所有Scheme
            //在preset_color_schemes节点下,获取所有Scheme节点
            Yaml.Node presetColorSchemes = _yamlUserWeaselFile.FindNodeByKey("preset_color_schemes");
            List<Yaml.Node> allSchemesNode = _yamlUserWeaselFile.NodeList.Where(n => n.Parent == presetColorSchemes).ToList();
            //遍历Scheme节点，获取出Scheme的皮肤信息
            foreach (Yaml.Node item in allSchemesNode)
            {
                List<Yaml.Node> schemeInfo = _yamlUserWeaselFile.NodeList.Where(n => n.Parent == item).ToList();

                Scheme scheme = new Scheme {id = item.Name};
                //皮肤id
                foreach (Yaml.Node infoItem in schemeInfo)
                {
                    switch (infoItem.Name)
                    {
                        case "name":
                            scheme.name = Tools.RemoveFirstLastQuotationMarks(infoItem.Value.Trim());
                            break;
                        case "author":
                            scheme.author = Tools.RemoveFirstLastQuotationMarks(infoItem.Value.Trim());
                            break;
                        case "back_color":
                            scheme.back_color = Tools.Bgr2Rgb(infoItem.Value.Trim());
                            break;
                        case "border_color":
                            scheme.border_color = Tools.Bgr2Rgb(infoItem.Value.Trim());
                            break;
                        case "text_color":
                            scheme.text_color = Tools.Bgr2Rgb(infoItem.Value.Trim());
                            break;
                        case "hilited_text_color":
                            scheme.hilited_text_color = Tools.Bgr2Rgb(infoItem.Value.Trim());
                            break;
                        case "hilited_back_color":
                            scheme.hilited_back_color = Tools.Bgr2Rgb(infoItem.Value.Trim());
                            break;
                        case "hilited_candidate_back_color":
                            scheme.hilited_candidate_back_color = Tools.Bgr2Rgb(infoItem.Value.Trim());
                            break;
                        case "hilited_candidate_text_color":
                            scheme.hilited_candidate_text_color = Tools.Bgr2Rgb(infoItem.Value.Trim());
                            break;
                        case "candidate_text_color":
                            scheme.candidate_text_color = Tools.Bgr2Rgb(infoItem.Value.Trim());
                            break;
                        case "comment_text_color":
                            scheme.comment_text_color = Tools.Bgr2Rgb(infoItem.Value.Trim());
                            break;
                    }
                }
                //判断是否rime预设Scheme：判断在安装目录data下 weasel.yaml 是否存在该Scheme Id，存在就是系统预设Scheme，否则就是自定义Scheme
                //weasel.custom.yaml  中为 "preset_color_schemes/myTheme"
                List<Yaml.Node> schemeUer = _yamlRootWeaselFile.NodeList.Where(n => n.Name == scheme.id).ToList();
                if (schemeUer.Count == 1)
                {
                    scheme.isSysScheme = true;
                }
                //添加到_listScheme
                _listScheme.Add(scheme);
            }

            //读取当前正在使用的Scheme
            _usingSchemeId = _yamlUserWeaselFile.Read("style.color_scheme");
            Scheme usingScheme = null;
            if (!string.IsNullOrEmpty(_usingSchemeId))
            {
                _usingSchemeId = _usingSchemeId.Trim();

            }
            else
            {
                _usingSchemeId = "aqua";
            }

            try
            {
                usingScheme = _listScheme.Single(s => s.id == _usingSchemeId);
                usingScheme.isUsing = true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            //绑定ListBoxSchemes
            ListBoxSchemes.ItemsSource = _listScheme;

            if (usingScheme == null)
            {
                usingScheme = _listScheme.Count > 0 ? _listScheme[0] : null;
            }

            //显示正在使用的SchemeId 
            if (usingScheme != null)
            {
                TxtUsingScheme.Text = usingScheme.name;
                //选中当前使用的皮肤
                ListBoxSchemes.SelectedItem = usingScheme;
                //将滚动条滚动到当前使用的皮肤
                ListBoxSchemes.ScrollIntoView(usingScheme);
                ListBoxSchemes.ScrollIntoView(usingScheme);
            }
        }

        /// <summary>
        /// 输入法方案 Schema载入
        /// </summary>
        private void LoadSchemas(object sender, DoWorkEventArgs e)
        {
            //===读取 安装目录\data\ 下所有  *.schema.yaml文件获取所有方案
            List<Schema> installSchemaList = ReadAllSchemaYaml(_strRootFolderPath + "\\data", true, false);

            //===读取用户配置目录下 下所有  *.schema.yaml文件
            List<Schema> userSchemaListT = ReadAllSchemaYaml(_strUserFolderPath + "\\build", false, false);

            //Linq not in查询出 在userSchemaListT列表中而不在installSchemaList列表中的 Schema，就是用户自行添加的Schema
            List<Schema> userSchemaList = (from tbUserSchemaListT in userSchemaListT
                                           where !(from tbInstallSchemaList in installSchemaList select tbInstallSchemaList.SchemaId).Contains(tbUserSchemaListT.SchemaId)
                                           select tbUserSchemaListT).ToList();
            foreach (Schema item in userSchemaList)
            {
                //item.isSys = false;//非Rime自带Schema
                installSchemaList.Add(item);
            }

            //=====处理自动纠错功能 translator/enable_correction========
            //2019年5月13日22:38:03
            //查询出读取用户配置目录下 下所有  *.schema.yaml文件 中启用了自动纠错功能的Schema
            List<Schema> enableCorrectionList = (from tbUserSchemaListT in userSchemaListT
                where tbUserSchemaListT.EnableCorrection
                select tbUserSchemaListT).ToList();
            //遍历installSchemaList找到enableCorrectionList中存在的Schema，启用自动纠错功能 EnableCorrection = true;
            foreach (Schema item in enableCorrectionList)
            {
                foreach (Schema itemG in installSchemaList)
                {
                    if (itemG.SchemaId==item.SchemaId)
                    {
                        itemG.EnableCorrection = true;
                        itemG.OldEnableCorrection = true;
                        break;
                    }
                }
            }
            
            //===读取用户Roaming\Rime目录下的配置文件
            if (Directory.Exists(_userRoamingFolderRime))
            {
                List<Schema> roamingSchemaList = ReadAllSchemaYaml(_userRoamingFolderRime, false, true);
                //Linq not in查询出 在roamingSchemaList列表中而不在installSchemaList列表中的 Schema，就是用户自行添加的Schema
                List<Schema> rsT = (from tbRst in roamingSchemaList
                                    where !(from tbInstallSchemaList in installSchemaList select tbInstallSchemaList.SchemaId).Contains(tbRst.SchemaId)
                                    select tbRst).ToList();
                foreach (Schema item in rsT)
                {
                    //item.isSys = false;//非Rime自带Schema
                    installSchemaList.Add(item);
                }
            }

            _listSchemaList = new ObservableCollection<Schema>(installSchemaList);
            //====读取用户正在使用的Schema id  用户目录下build\default.yaml key:schema_list
            Yaml.Node nodeSchemaList = _yamlUserDefaultFile.FindNodeByKey("schema_list");
            List<Yaml.Node> schemaList = _yamlUserDefaultFile.NodeList.Where(n => n.Parent == nodeSchemaList).ToList();
            //批量修改_listSchemaList中正在使用的
            (from tbListSchemaList in _listSchemaList
             where (from tbSchemaList in schemaList select tbSchemaList.Value).Contains(tbListSchemaList.SchemaId)
             select tbListSchemaList)
             .ToList()
             .ForEach(sScheme => { sScheme.IsUsing = true; sScheme.IsSelect = true; });
        }

        /// <summary>
        /// 方案信息载入完成时执行
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LoadSchemasCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //设置 DataGridSchema 的数据源为 _listSchemaList
            DataGridSchema.ItemsSource = _listSchemaList;
        }


        private List<Schema> ReadAllSchemaYaml(string dirPath, bool isSys, bool inRoaming)
        {
            List<Schema> listR = new List<Schema>();
            if (Directory.Exists(dirPath))
            {
                DirectoryInfo directory = new DirectoryInfo(dirPath);
                FileInfo[] files = directory.GetFiles();
                foreach (FileInfo file in files)
                {
                    //读取一.schema.yaml结尾的文件
                    if (file.Name.EndsWith(".schema.yaml"))
                    {
                        //if (file.Name == "bopomofo.schema.yaml")
                        //{
                        //    Console.WriteLine("");
                        //}

                        Schema schema = new Schema();

                        Yaml tempSchemaYaml = new Yaml(file.FullName);
                        //Schema id
                        try
                        {

                            string tId = tempSchemaYaml.Read("schema.schema_id").Split('#')[0].Trim();
                            schema.SchemaId = tId;
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }
                        //Schema 名称
                        schema.Name = Tools.RemoveFirstLastQuotationMarks(tempSchemaYaml.Read("schema.name"));
                        //Schema 版本
                        schema.Version = Tools.RemoveFirstLastQuotationMarks(tempSchemaYaml.Read("schema.version"));

                        List<Yaml.Node> tNodes; //临时记录信息子节点
                        //作者信息
                        Yaml.Node author = tempSchemaYaml.FindNodeByKey("schema.author");
                        if (author != null)
                        {
                            tNodes = tempSchemaYaml.NodeList.Where(n => n.Parent == author).ToList();
                            foreach (Yaml.Node item in tNodes)
                            {
                                schema.Author += item.Name.Trim() + "\r\n";
                            }
                            schema.Author = schema.Author.Substring(0, schema.Author.Count() - 2);
                        }
                        //description 描述信息
                        Yaml.Node description = tempSchemaYaml.FindNodeByKey("schema.description");
                        if (description != null)
                        {
                            tNodes = tempSchemaYaml.NodeList.Where(n => n.Parent == description).ToList();
                            foreach (Yaml.Node item in tNodes)
                            {
                                schema.Description += item.Name.Trim() + "\r\n";
                                List<Yaml.Node> ttNodes = tempSchemaYaml.NodeList.Where(n => n.Parent == item).ToList();
                                foreach (Yaml.Node ttitem in ttNodes)
                                {
                                    schema.Description += "  " + ttitem.Name.Trim() + "\r\n";
                                }
                                //schema.description = schema.description.Substring(0, schema.description.Count() - 2);
                            }
                        }

                        //dependencies 依赖情况
                        Yaml.Node dependencies = tempSchemaYaml.FindNodeByKey("schema.dependencies");
                        if (dependencies != null)
                        {
                            tNodes = tempSchemaYaml.NodeList.Where(n => n.Parent == dependencies).ToList();
                            foreach (Yaml.Node item in tNodes)
                            {
                                schema.Dependencies += item.Name.Replace("- ", "").Trim() + ";";
                            }
                            schema.Dependencies = schema.Dependencies.Substring(0, schema.Dependencies.Count() - 1);
                        }

                        //读取 translator/enable_correction
                        string enableCorrection = tempSchemaYaml.Read("translator.enable_correction");
                        if (!string.IsNullOrEmpty(enableCorrection))
                        {
                            schema.EnableCorrection = true;
                        }

                        schema.IsSys = isSys;
                        schema.InRoaming = inRoaming;
                        listR.Add(schema);
                    }
                }
            }
            return listR;
        }

        /// <summary>
        /// 加载备份信息列表
        /// </summary>
        private void LoadBackInfo()
        {
            //检查备份目录，没有就创建
            if (!Directory.Exists(_strBackupsFolder)) Directory.CreateDirectory(_strBackupsFolder);
            _dgBackAndRestoreItem = new ObservableCollection<BackAndRestoreItems>();
            //获取备份目录下所有的文件
            string[] strFiles = Directory.GetFiles(_strBackupsFolder);
            //遍历文件信息
            foreach (string file in strFiles)
            {
                string filesName = Regex.Match(file, "(20\\d+\\.zip)").Value;
                string fileTimeYmd = Regex.Match(filesName, "20\\d{2}[01]\\d[0123]\\d").Value;
                fileTimeYmd = Regex.Replace(fileTimeYmd, "(.{4})(.{2})(.{2})", "$1-$2-$3");
                string fileTimeHms = Regex.Match(filesName, "(?<=(20\\d{2}[01]\\d[0123]\\d))[012]\\d[012345]\\d[012345]\\d").Value;
                fileTimeHms = Regex.Replace(fileTimeHms, "(.{2})(.{2})(.{2})", "$1:$2:$3");
                fileTimeYmd += " " + fileTimeHms;
                FileInfo myFileInfo = new FileInfo(file);
                float fileSize = Convert.ToSingle(myFileInfo.Length / 1024.0 / 1024.0);

                _dgBackAndRestoreItem.Add(new BackAndRestoreItems
                {
                    ItemFileName = filesName,
                    ItemFileTime = fileTimeYmd,
                    ItemFileSize = fileSize
                });
            }

            DataGridBack.ItemsSource = _dgBackAndRestoreItem;
        }

        #endregion

        #region 全局部分 控件事件-载入、保存、重新载入
        /// <summary>
        /// 窗体载入事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SetControl();
            ReadRimeRegistry();           //通过注册表获取小狼毫输入法的用户目录
            BindingComboBox();            //常规页面绑定ComboBox
            ProfileLoad();                //配置文件载入

            SettingLoad();              //设置数据读取绑定

            //加载版本信息
            string version = Properties.Resources.ResourceManager.GetString("app_version");
            TbVersionAbout.Text = "版本：V" + version;
            LblVersion.Content = "RimeControl V" + version;

            //检查版本号，是否有更新
            BackgroundWorker bwCheckReleases = new BackgroundWorker();
            bwCheckReleases.DoWork += CheckReleases;//执行的任务
            bwCheckReleases.RunWorkerCompleted += CheckReleasesCompleted;//任务执行完成后的回调
            bwCheckReleases.RunWorkerAsync();//开始执行后台任务
        }

        /// <summary>
        /// 检查版本号后执行
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckReleasesCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_newVersion))
            {
                //判断新版本号大于旧版本，显示新版本号
                if (String.Compare(_newVersion, Properties.Resources.ResourceManager.GetString("app_version"), StringComparison.Ordinal) > 0)
                {
                    BtnNewVersion.Content = "有新版本：V" + _newVersion;
                    BtnNewVersion.Visibility = Visibility.Visible;
                }
            }
        }
        /// <summary>
        /// 检查版本号
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckReleases(object sender, DoWorkEventArgs e)
        {
            string url = "https://api.github.com/repos/qzly/RimeControl/releases/latest";
            string result = HttpUtils.GetStringData(url, null);
            Regex regexTagName = new Regex("(?<=tag_name\" *: *\")\\w+?(?=\",)");
            Match match = regexTagName.Match(result);
            if (match.Success)
            {
                _newVersion = match.Value;
            }
        }


        /// <summary>
        /// 保存按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {

            //打开屏蔽提示层
            LpLoading.Visibility = Visibility.Visible;//显示
            LpLoading.LoadingText = "保存配置中...";

            try
            {
                #region 常规部分
                //字体
                string strFonts = TextFonts.Text.Trim();
                strFonts = "\"" + strFonts + "\"";
                Tools.SetYamlValue(_yamlUserWeaselCustomFile, "\"style/font_face\"", strFonts);
                //字号
                string fontSize = CobFontSize.SelectedValue.ToString();
                Tools.SetYamlValue(_yamlUserWeaselCustomFile, "\"style/font_point\"", fontSize);
                //每页候选个数
                string strPageSize = CobPageSize.SelectedValue.ToString().ToLower();
                Tools.SetYamlValue(_yamlUserDefaultCustomFile, "\"menu/page_size\"", strPageSize);
                //候选项横/竖排显示
                string strHorizontal = RdbHorizontal.IsChecked.ToString().ToLower();
                Tools.SetYamlValue(_yamlUserWeaselCustomFile, "\"style/horizontal\"", strHorizontal);
                //托盘图标
                Tools.SetYamlValue(_yamlUserWeaselCustomFile, "\"style/display_tray_icon\"", CkbDisplayTrayIcon.IsChecked.ToString().ToLower());
                //内嵌编码
                Tools.SetYamlValue(_yamlUserWeaselCustomFile, "\"style/inline_preedit\"", CkbInlinePreedit.IsChecked.ToString().ToLower());
                //内嵌全屏
                Tools.SetYamlValue(_yamlUserWeaselCustomFile, "\"style/fullscreen\"", CkbFullScreen.IsChecked.ToString().ToLower());
                //good_old_caps_lock
                Tools.SetYamlValue(_yamlUserDefaultCustomFile, "\"ascii_composer/good_old_caps_lock\"", CkbGoodOldCapsLock.IsChecked.ToString().ToLower());

                //===switch_key
                //Caps_Lock
                Tools.SetYamlValue(_yamlUserDefaultCustomFile, "\"ascii_composer/switch_key/Caps_Lock\"", _dirCobCtrlLf[Convert.ToInt32(CobCopsLock.SelectedValue)]);
                //Control_L
                Tools.SetYamlValue(_yamlUserDefaultCustomFile, "\"ascii_composer/switch_key/Control_L\"", _dirCobCtrlLf[Convert.ToInt32(CobCtrlL.SelectedValue)]);
                //Control_R
                Tools.SetYamlValue(_yamlUserDefaultCustomFile, "\"ascii_composer/switch_key/Control_R\"", _dirCobCtrlLf[Convert.ToInt32(CobCtrlR.SelectedValue)]);
                //Eisu_toggle
                Tools.SetYamlValue(_yamlUserDefaultCustomFile, "\"ascii_composer/switch_key/Eisu_toggle\"", _dirCobCtrlLf[Convert.ToInt32(CobEisutoggle.SelectedValue)]);
                //Shift_L
                Tools.SetYamlValue(_yamlUserDefaultCustomFile, "\"ascii_composer/switch_key/Shift_L\"", _dirCobCtrlLf[Convert.ToInt32(CobShiftL.SelectedValue)]);
                //Shift_R
                Tools.SetYamlValue(_yamlUserDefaultCustomFile, "\"ascii_composer/switch_key/Shift_R\"", _dirCobCtrlLf[Convert.ToInt32(CobShiftR.SelectedValue)]);

                //方案选单显示名
                string caption = TxtFangAnName.Text.Trim();
                caption = "\"" + caption + "\"";
                Tools.SetYamlValue(_yamlUserDefaultCustomFile, "\"switcher/caption\"", caption);

                //=========switcher.hotkeys
                bool enable1 = CkbHotKey1.IsChecked != null && CkbHotKey1.IsChecked.Value;
                bool enable2 = CkbHotKey2.IsChecked != null && CkbHotKey2.IsChecked.Value;
                bool enable3 = CkbHotKey3.IsChecked != null && CkbHotKey3.IsChecked.Value;
                bool enable4 = CkbHotKey4.IsChecked != null && CkbHotKey4.IsChecked.Value;

                string strHotKey1 = TxtHotKey1.Text.Trim();
                string strHotKey2 = TxtHotKey2.Text.Trim();
                string strHotKey3 = TxtHotKey3.Text.Trim();
                string strHotKey4 = TxtHotKey4.Text.Trim();

                strHotKey1 = "- " + strHotKey1;
                strHotKey2 = "- " + strHotKey2;
                strHotKey3 = "- " + strHotKey3;
                strHotKey4 = "- " + strHotKey4;

                //先移除 default.custom.yaml 中 patch."switcher/hotkeys"节点的所有子节点
                Yaml.Node switcherHotkeys = _yamlUserDefaultCustomFile.FindNodeByKey("patch.\"switcher/hotkeys\"");
                if (switcherHotkeys != null)
                {
                    List<Yaml.Node> removeNodes = _yamlUserDefaultCustomFile.NodeList.Where(rn => rn.Parent == switcherHotkeys).ToList();
                    foreach (Yaml.Node removeNode in removeNodes)
                    {
                        _yamlUserDefaultCustomFile.NodeList.Remove(removeNode);
                    }
                }
                //添加
                string hotkeys_keys = "patch.\"switcher/hotkeys\".";
                if (enable1)
                {
                    _yamlUserDefaultCustomFile.Add(hotkeys_keys + strHotKey1, "", true);
                }
                if (enable2)
                {
                    _yamlUserDefaultCustomFile.Add(hotkeys_keys + strHotKey2, "", true);
                }
                if (enable3)
                {
                    _yamlUserDefaultCustomFile.Add(hotkeys_keys + strHotKey3, "", true);
                }
                if (enable4)
                {
                    _yamlUserDefaultCustomFile.Add(hotkeys_keys + strHotKey4, "", true);
                }

                #endregion

                #region Scheme部分

                //----weasel.custom.yaml
                //===自定义皮肤保存 patch.preset_color_schemes.mytheme
                string[] keys = new string[] { "name", "author", "back_color", "border_color", "text_color", "hilited_text_color", "hilited_back_color", "hilited_candidate_back_color"
            ,"hilited_candidate_text_color","candidate_text_color","comment_text_color"};
                //==在原来的原来的List中查询出旧的Scheme数据并移除
                //在patch.preset_color_schemes节点下,获取所有Scheme节点
                List<Yaml.Node> oldSchemesNode = _yamlUserWeaselCustomFile.NodeList.Where(n => n.Name.Contains("preset_color_schemes/")).ToList();
                foreach (Yaml.Node oldItem in oldSchemesNode)
                {
                    string oldSchemeId = oldItem.Name.Trim();
                    foreach (string oldKey in keys)
                    {
                        string removeKey = "patch." + oldSchemeId + "." + oldKey;
                        _yamlUserWeaselCustomFile.Remove(removeKey);
                    }
                    _yamlUserWeaselCustomFile.Remove("patch." + oldSchemeId);
                }
                //==把 _listScheme 中的非预设Scheme保存到weasel.custom.yaml
                List<Scheme> customSchemes = _listScheme.Where(s => s.isSysScheme == false).ToList();
                foreach (Scheme customScheme in customSchemes)
                {
                    string addKey = "patch.\"preset_color_schemes/" + customScheme.id.Trim() + "\".";
                    _yamlUserWeaselCustomFile.Add(addKey + "name", customScheme.name.Trim(), false);
                    _yamlUserWeaselCustomFile.Add(addKey + "author", customScheme.author.Trim(), false);

                    _yamlUserWeaselCustomFile.Add(addKey + "back_color", Tools.Rgb2Bgr(customScheme.back_color), false);
                    _yamlUserWeaselCustomFile.Add(addKey + "border_color", Tools.Rgb2Bgr(customScheme.border_color), false);

                    _yamlUserWeaselCustomFile.Add(addKey + "text_color", Tools.Rgb2Bgr(customScheme.text_color), false);
                    _yamlUserWeaselCustomFile.Add(addKey + "hilited_text_color", Tools.Rgb2Bgr(customScheme.hilited_text_color), false);
                    _yamlUserWeaselCustomFile.Add(addKey + "hilited_back_color", Tools.Rgb2Bgr(customScheme.hilited_back_color), false);

                    _yamlUserWeaselCustomFile.Add(addKey + "hilited_candidate_back_color", Tools.Rgb2Bgr(customScheme.hilited_candidate_back_color), false);
                    _yamlUserWeaselCustomFile.Add(addKey + "hilited_candidate_text_color", Tools.Rgb2Bgr(customScheme.hilited_candidate_text_color), false);

                    _yamlUserWeaselCustomFile.Add(addKey + "candidate_text_color", Tools.Rgb2Bgr(customScheme.candidate_text_color), false);
                    _yamlUserWeaselCustomFile.Add(addKey + "comment_text_color", Tools.Rgb2Bgr(customScheme.comment_text_color), false);
                }

                //========保存 patch.style.color_scheme  使用的Scheme id
                //List<Scheme> li = new List<Scheme>(_listScheme.ToList());//--调试用于查看结果
                Scheme usingScheme = _listScheme.Single(s => s.isUsing);
                Tools.SetYamlValue(_yamlUserWeaselCustomFile, "\"style/color_scheme\"", usingScheme.id.Trim());

                #endregion

                #region 输入法方案 Schema 部分

                /**
                 * 2019年2月26日20:28:59
                 * 修复在初始配置下，第一次配置时（patch.schema_list不存在时）以下配置信息无法保存
                 * menu/page_size 
                 * ascii_composer/good_old_caps_lock
                 * ascii_composer/switch_key/Caps_Lock
                 * ascii_composer/switch_key/Control_L
                 * ascii_composer/switch_key/Control_R
                 * ascii_composer/switch_key/Eisu_toggle
                 * ascii_composer/switch_key/Shift_L
                 * ascii_composer/switch_key/Shift_R
                 * switcher/caption
                 */
                //移除default.custom.yaml 中 patch.schema_list 的所有子节点
                Yaml.Node schemaListNode = _yamlUserDefaultCustomFile.FindNodeByKey("patch.schema_list");
                if (schemaListNode != null)
                {
                    List<Yaml.Node> removeSchemaNodes = _yamlUserDefaultCustomFile.NodeList.Where(rn => rn.Parent == schemaListNode).ToList();
                    foreach (Yaml.Node removeNode in removeSchemaNodes)
                    {
                        _yamlUserDefaultCustomFile.NodeList.Remove(removeNode);
                    }
                }

                //将勾选的Schema 的id写入default.custom.yaml 中 patch.schema_list
                List<Schema> saveSchemas = _listSchemaList.Where(schema => schema.IsSelect).ToList();

                // 2019-3-7 19:14 修复全新配置第一次使用RimeControl后输入法被切换为“注音”的问题
                //将 luna_pinyin 与列表的第一位对调
                SchemaListOrder(saveSchemas, "luna_pinyin",0);
                //将 luna_pinyin_fluency 与列表的第二位对调
                SchemaListOrder(saveSchemas, "luna_pinyin_fluency", 1);
                //将 luna_pinyin_simp 与列表的第三位对调
                SchemaListOrder(saveSchemas, "luna_pinyin_simp", 2);

                foreach (Schema saveSchema in saveSchemas)
                {
                    //如果在 Roaming\Rime目录中把它复制到 用户配置文件夹
                    if (saveSchema.InRoaming)
                    {
                        string schemaFileFromPath = _userRoamingFolderRime + "\\" + saveSchema.SchemaId + ".schema.yaml";
                        string schemaFileToPath = _strUserFolderPath + "\\" + saveSchema.SchemaId + ".schema.yaml";
                        //判断用户目录是否就是  Roaming\Rime目录；如果不是就不再复制文件，否则复制文件
                        if (_userRoamingFolderRime != _strUserFolderPath && File.Exists(schemaFileFromPath))
                        {
                            try
                            {
                                File.Copy(schemaFileFromPath, schemaFileToPath, false);
                            }
                            catch (Exception exception)
                            {
                                Console.WriteLine(exception);
                            }
                        }
                    }
                    _yamlUserDefaultCustomFile.Add("patch.schema_list.- schema: " + saveSchema.SchemaId, "", true);
                }


                /**
                 * 2019-5-13 23:28:09
                 * 保存Schema的自动纠错功能
                 * 实现：向SchemaName.custom.yaml文件中patch节点下添加    "translator/enable_correction": true
                 */
                 //查询出修改前启用自动纠错功能和修改后启用自动纠错功能的Schema列表
                 List<Schema> correctionSchemas=_listSchemaList.Where(schema=>schema.OldEnableCorrection||schema.EnableCorrection).ToList();
                //遍历correctionSchemas列表
                foreach (Schema correctionSchema in correctionSchemas)
                {
                    string customYamlPath = _strUserFolderPath + "//" + correctionSchema.SchemaId + ".custom.yaml";
                    if (!correctionSchema.OldEnableCorrection && correctionSchema.EnableCorrection)
                    {
                        //修改前没有启用，修改后启用了自动纠错功能，需要在.custom.yaml中添加 "translator/enable_correction": true
                        Yaml customYaml=new Yaml(customYamlPath,true);
                        customYaml.Add("patch.\"translator/enable_correction\"", "true", false);
                        customYaml.Save();
                    }
                    else if (correctionSchema.OldEnableCorrection && !correctionSchema.EnableCorrection)
                    {
                        //修改前启用，修改后没有启用。需要在.custom.yaml中移除 "translator/enable_correction": true
                        if (File.Exists(customYamlPath))
                        {
                            Yaml customYaml = new Yaml(customYamlPath);
                            customYaml.Remove("patch.\"translator/enable_correction\"");
                            customYaml.Save();
                        }
                    }
                    else
                    {
                        //修改前后都启用 或者 修改前后都没有启用的情况，，不做处理
                    }
                }


                #endregion

                //保存设置信息
                _yamlUserDefaultCustomFile.Save();
                _yamlUserWeaselCustomFile.Save();

                DeployAndWait();// 小狼毫重新部署并等待
            }
            catch (Exception ex)
            {
                MessageBox.Show("保存失败！程序Log目录下最新的日志文件记录了错误代码，请联系开发者 Email:1396715343@qq.com。考虑不周，抱歉。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                ExceptionLog.WriteLog(ex);
            }
        }

        /// <summary>
        /// SchemaList Schema顺序调整
        /// 为修复全新配置第一次使用RimeControl后输入法被切换为“注音”的问题
        /// </summary>
        /// <param name="listSchema">Schema 列表</param>
        /// <param name="schemaId">要调整Schema 的id</param>
        /// <param name="goalIndex">调整的目标索引</param>
        private void SchemaListOrder(List<Schema> listSchema, string schemaId,int goalIndex)
        {
            try
            {
                List<Schema> searchListSchemas = listSchema.FindAll(s => s.SchemaId == schemaId);
                if (searchListSchemas.Count>0)
                {
                    Schema schema = searchListSchemas[0];
                    int lunaPinyinIndex = listSchema.IndexOf(schema);
                    //判断 schemaId 是否在 目标位置，如果不是对调位置
                    if (lunaPinyinIndex != goalIndex)
                    {
                        listSchema[lunaPinyinIndex] = listSchema[goalIndex];
                        listSchema[goalIndex] = schema;
                    }
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        /// <summary>
        /// 重新载入按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnReLoad_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("是否要放弃修改，重新载入配置信息？", "提示", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                ProfileLoad();                //配置文件载入

                SettingLoad();              //设置数据读取绑定
            }
        }
        #endregion

        #region 常规设置部分 事件
        /// <summary>
        /// 字体选择按钮 打开字体选择对话框
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnFonts_Click(object sender, RoutedEventArgs e)
        {
            /**
             * FontDialogSample 来自:https://github.com/feilongsword/FontDialogSample
             * 修改了一下界面
             **/
            FontDialogSample.FontChooser myFontChooser = new FontDialogSample.FontChooser { Owner = this };

            myFontChooser.SetPropertiesFromObject(TextFonts);
            myFontChooser.PreviewSampleText = TextFonts.SelectedText;
            //打开字体选择对话框
            bool? showDialog = myFontChooser.ShowDialog();
            if (showDialog != null && showDialog.Value)
            {
                //应用选择的字体
                myFontChooser.ApplyPropertiesToObject(TextFonts);
            }
            //显示字体名称
            TextFonts.Text = TextFonts.FontFamily.ToString();
        }

        /// <summary>
        /// 快捷键选择框 捕获快捷键
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TxtHotKey_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            TextBox tb = sender as TextBox;
            string hotKey = "";
            //处理Ctrl
            if (e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Control))
            {
                //修饰键只要按下了Ctrl，不管按没按其他修饰键，都会进入
                hotKey += "Control";
            }
            //处理Shift
            if (e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                if (!string.IsNullOrEmpty(hotKey))
                {
                    hotKey += "+";
                }
                hotKey += "Shift";
            }
            //处理Alt
            if (e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Alt))
            {
                if (!string.IsNullOrEmpty(hotKey))
                {
                    hotKey += "+";
                }
                hotKey += "Alt";
            }
            //一般按钮
            if (e.Key != Key.LeftCtrl && e.Key != Key.RightCtrl
                && e.Key != Key.LeftShift && e.Key != Key.RightShift
                && e.Key != Key.LeftAlt && e.Key != Key.RightAlt
                && e.Key != Key.LWin && e.Key != Key.RWin)
            {
                if (!string.IsNullOrEmpty(hotKey))
                {
                    hotKey += "+";
                }
                if ((e.Key >= Key.A && e.Key <= Key.Z) || (e.Key >= Key.F1 && e.Key <= Key.F12))
                {
                    hotKey += e.Key.ToString();
                }
                else
                {
                    hotKey += Properties.Resources.ResourceManager.GetString(e.Key.ToString());
                }
            }

            if (tb != null) tb.Text = hotKey;
            e.Handled = true;
        }

        #endregion

        #region 皮肤Scheme部分 事件
        /// <summary>
        /// ListBoxSchemes ListBox选择改变事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ListBoxSchemes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //避免selectedScheme为空
            if (ListBoxSchemes.SelectedItem is Scheme selectedScheme)
            {
                _schemeInfoLoading = true;//开始加载Scheme信息

                //绑定信息
                TxtSchemeId.Text = selectedScheme.id;
                TxtSchemeName.Text = selectedScheme.name;
                TxtSchemeAuthor.Text = selectedScheme.author;

                ColorBack.SelectedColor = selectedScheme.back_color;
                ColorBorder.SelectedColor = selectedScheme.border_color;

                ColorText.SelectedColor = selectedScheme.text_color;
                ColorHilitedText.SelectedColor = selectedScheme.hilited_text_color;
                ColorHilitedBack.SelectedColor = selectedScheme.hilited_back_color;

                ColorHilitedCandidateText.SelectedColor = selectedScheme.hilited_candidate_text_color;
                ColorHilitedCandidateBack.SelectedColor = selectedScheme.hilited_candidate_back_color;

                ColorCandidateText.SelectedColor = selectedScheme.candidate_text_color;
                ColorCommentText.SelectedColor = selectedScheme.comment_text_color;

                //判断是否rime预设，预设禁止编辑，删除按钮
                if (selectedScheme.isSysScheme)
                {
                    TxtSchemeId.IsReadOnly = true;
                    TxtSchemeName.IsReadOnly = true;
                    TxtSchemeAuthor.IsReadOnly = true;

                    ColorBack.IsEnabled = false;
                    ColorBorder.IsEnabled = false;

                    ColorText.IsEnabled = false;
                    ColorHilitedText.IsEnabled = false;
                    ColorHilitedBack.IsEnabled = false;

                    ColorHilitedCandidateText.IsEnabled = false;
                    ColorHilitedCandidateBack.IsEnabled = false;

                    ColorCandidateText.IsEnabled = false;
                    ColorCommentText.IsEnabled = false;
                    //禁止删除按钮
                    DelScheme.IsEnabled = false;
                    //显示预设Scheme标识
                    LalSysScheme.Visibility = Visibility.Visible;
                }
                else
                {
                    //启用编辑
                    TxtSchemeId.IsReadOnly = false;
                    TxtSchemeName.IsReadOnly = false;
                    TxtSchemeAuthor.IsReadOnly = false;

                    ColorBack.IsEnabled = true;
                    ColorBorder.IsEnabled = true;

                    ColorText.IsEnabled = true;
                    ColorHilitedText.IsEnabled = true;
                    ColorHilitedBack.IsEnabled = true;

                    ColorHilitedCandidateText.IsEnabled = true;
                    ColorHilitedCandidateBack.IsEnabled = true;

                    ColorCandidateText.IsEnabled = true;
                    ColorCommentText.IsEnabled = true;
                    //启用删除按钮
                    DelScheme.IsEnabled = true;
                    //隐藏预设Scheme标识
                    LalSysScheme.Visibility = Visibility.Hidden;
                }
                _schemeInfoLoading = false;//加载Scheme信息 结束
            }
        }

        /// <summary>
        /// 新增皮肤
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddNewScheme_Click(object sender, RoutedEventArgs e)
        {
            string strId = "NewScheme_" + (++_newSchemeIndex).ToString();
            Scheme newScheme = new Scheme
            {
                id = strId,
                name = strId,
                author = "RimeControl",

                back_color = Color.FromRgb(255, 255, 255),
                border_color = Color.FromRgb(0, 0, 0),

                text_color = Color.FromRgb(0, 0, 0),
                hilited_text_color = Color.FromRgb(0, 0, 0),
                hilited_back_color = Color.FromRgb(255, 255, 255),

                hilited_candidate_back_color = Color.FromRgb(255, 255, 255),
                hilited_candidate_text_color = Color.FromRgb(0, 0, 0),

                candidate_text_color = Color.FromRgb(0, 0, 0),
                comment_text_color = Color.FromRgb(0, 0, 0),

                isNew = true,//新加的Scheme
            };
            _listScheme.Add(newScheme);
            //选择当前的皮肤
            ListBoxSchemes.SelectedItem = newScheme;
            //将滚动条滚动到新建的Scheme
            ListBoxSchemes.ScrollIntoView(newScheme);
        }

        /// <summary>
        /// 复制皮肤
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CopyScheme_Click(object sender, RoutedEventArgs e)
        {
            if (ListBoxSchemes.SelectedItem is Scheme selectedScheme)
            {
                Scheme newScheme = selectedScheme.Clone();//克隆
                newScheme.id = "copy_" + newScheme.id;
                newScheme.name = "copy_" + newScheme.name;
                newScheme.isNew = true;
                _listScheme.Add(newScheme);
                //选择当前的皮肤
                ListBoxSchemes.SelectedItem = newScheme;
                //将滚动条滚动到新建的Scheme
                ListBoxSchemes.ScrollIntoView(newScheme);
            }
            else
            {
                MessageBox.Show("请选择需要复制的Scheme！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// 删除皮肤
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DelScheme_Click(object sender, RoutedEventArgs e)
        {
            if (ListBoxSchemes.SelectedItem is Scheme selectedScheme)
            {
                if (MessageBox.Show("确定要删除的Scheme: " + selectedScheme.name + " 吗？", "提示", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    _listScheme.Remove(selectedScheme);//移除选中项

                    //跳转到当前使用的Scheme
                    try
                    {
                        Scheme usingScheme;
                        //判断移除项是不是当前使用Scheme
                        if (selectedScheme.isUsing)
                        {
                            //当前使用Scheme --把 aqua设为当前使用皮肤
                            usingScheme = _listScheme.Single(s => s.id == "aqua");
                            usingScheme.isUsing = true;
                            TxtUsingScheme.Text = usingScheme.name;
                        }
                        else
                        {
                            usingScheme = _listScheme.Single(s => s.isUsing);
                        }
                        //选择当前使用的皮肤
                        ListBoxSchemes.SelectedItem = usingScheme;
                        //将滚动条滚动到当前使用的皮肤
                        ListBoxSchemes.ScrollIntoView(usingScheme);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }
            }
            else
            {
                MessageBox.Show("请选择要删除的Scheme！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            //List<Scheme> li = new List<Scheme>(_listScheme.ToList()); //--调试用于查看结果
        }

        /// <summary>
        /// Scheme信息编辑（id，name，author）改变事件
        /// 改变后直接更新列表中的Scheme信息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SchemeTxt_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_schemeInfoLoading)
            {
                //还在加载Scheme信息过程中，不需要执行以下方法
                return;
            }

            if (ListBoxSchemes.SelectedItem is Scheme usingScheme)
            {
                if (usingScheme.isSysScheme == false)
                {
                    //获取当前选中的Scheme在_listScheme中索引，方便更新
                    int intSchemeIndex = _listScheme.IndexOf(usingScheme);

                    //或当前的TextBox
                    if (sender is TextBox currentTxt)
                    {
                        string txtName = currentTxt.Name.Trim();
                        switch (txtName)
                        {
                            case "TxtSchemeId":
                                _listScheme[intSchemeIndex].id = currentTxt.Text.Trim();
                                break;
                            case "TxtSchemeName":
                                _listScheme[intSchemeIndex].name = currentTxt.Text.Trim();
                                //判断当前皮肤是不是正在使用皮肤
                                if (_listScheme[intSchemeIndex].isUsing)
                                {
                                    //如果是，更新一下 使用皮肤文本框
                                    TxtUsingScheme.Text = _listScheme[intSchemeIndex].name;
                                }
                                break;
                            case "TxtSchemeAuthor":
                                _listScheme[intSchemeIndex].author = currentTxt.Text.Trim();
                                break;
                        }
                    }
                }
                else
                {
                    MessageBox.Show("当前选择的Scheme是系统预设的，不能编辑！可以复制后编辑副本。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            else
            {
                MessageBox.Show("被编辑的不是当前选择的Scheme，请重新选择要编辑的Scheme再编辑！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Scheme信息编辑 选择的颜色改变 改变事件
        /// 改变后直接更新列表中的Scheme信息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (_schemeInfoLoading)
            {
                //还在加载Scheme信息过程中，不需要执行以下方法
                return;
            }

            if (ListBoxSchemes.SelectedItem is Scheme usingScheme)
            {
                if (usingScheme.isSysScheme == false)
                {
                    //获取当前选中的Scheme在_listScheme中索引，方便更新
                    int intSchemeIndex = _listScheme.IndexOf(usingScheme);

                    //当前的TextBox
                    if (sender is ColorPicker currentColorPicker)
                    {
                        if (currentColorPicker.SelectedColor == null) return;

                        string txtName = currentColorPicker.Name.Trim();
                        switch (txtName)
                        {
                            case "ColorBack":
                                _listScheme[intSchemeIndex].back_color = currentColorPicker.SelectedColor.Value;
                                break;
                            case "ColorBorder":
                                _listScheme[intSchemeIndex].border_color = currentColorPicker.SelectedColor.Value;
                                break;

                            case "ColorText":
                                _listScheme[intSchemeIndex].text_color = currentColorPicker.SelectedColor.Value;
                                break;
                            case "ColorHilitedText":
                                _listScheme[intSchemeIndex].hilited_text_color = currentColorPicker.SelectedColor.Value;
                                break;
                            case "ColorHilitedBack":
                                _listScheme[intSchemeIndex].hilited_back_color = currentColorPicker.SelectedColor.Value;
                                break;
                            /*
                             * 2019-4-25 18:21:03 修复 ColorHilitedCandidateText 和 ColorHilitedCandidateBack代码写反
                             * 感谢 github 的 thisistianqi发现问题
                             */
                            case "ColorHilitedCandidateText":
                                _listScheme[intSchemeIndex].hilited_candidate_text_color = currentColorPicker.SelectedColor.Value;
                                break;
                            case "ColorHilitedCandidateBack":
                                _listScheme[intSchemeIndex].hilited_candidate_back_color = currentColorPicker.SelectedColor.Value;
                                break;

                            case "ColorCandidateText":
                                _listScheme[intSchemeIndex].candidate_text_color = currentColorPicker.SelectedColor.Value;
                                break;
                            case "ColorCommentText":
                                _listScheme[intSchemeIndex].comment_text_color = currentColorPicker.SelectedColor.Value;
                                break;
                        }
                    }
                }
                else
                {
                    MessageBox.Show("当前选择的Scheme是系统预设的，不能编辑！可以复制后编辑副本。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            else
            {
                MessageBox.Show("被编辑的不是当前选择的Scheme，请重新选择要编辑的Scheme再编辑！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// 设置使用皮肤
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnUseScheme_Click(object sender, RoutedEventArgs e)
        {
            if (ListBoxSchemes.SelectedItem is Scheme usingScheme)
            {
                foreach (var schemaItem in _listScheme)
                {
                    schemaItem.isUsing = (schemaItem.id == usingScheme.id);
                }

                TxtUsingScheme.Text = usingScheme.name;
                //List<Scheme> li = new List<Scheme>(_listScheme.ToList());--调试用于查看结果
            }
            else
            {
                MessageBox.Show("请选择需要使用的Scheme", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        #endregion

        #region 方案Schema部分 事件

        /// <summary>
        /// 启用否 单击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ColCkbSelect_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox ckbSelect && DataGridSchema.SelectedItems.Count > 0 && DataGridSchema.SelectedItems[0] is Schema currentSchema)
            {
                if (ckbSelect.IsChecked != null && ckbSelect.IsChecked.Value)
                {
                    //--勾选，检查依赖的Schema是否勾选
                    bool isAllSelect = true;
                    string strNoSelectYlSchema = "";
                    //获取所有currentSchema依赖或间接依赖的Schema
                    List<Schema> ylSchemas = new List<Schema>();
                    Tools.GetAllSchemaYl(_listSchemaList, ylSchemas, currentSchema);
                    //排除自身，有些情况下会出现自身
                    if (ylSchemas.IndexOf(currentSchema) > -1)
                    {
                        ylSchemas.Remove(currentSchema);
                    }
                    //遍历判断是否都勾选，取出没有勾选的
                    foreach (Schema ylSchema in ylSchemas)
                    {
                        if (!ylSchema.IsSelect)
                        {
                            isAllSelect = false;
                            strNoSelectYlSchema += ylSchema.SchemaId + " ";
                        }
                    }
                    if (!isAllSelect)
                    {
                        string strMsg = "该Schema:[" + currentSchema.SchemaId + "]依赖于或间接依赖于未启用的Schema:[" + strNoSelectYlSchema + "]" + ",将全部启用，是否继续启用?";
                        if (MessageBox.Show(strMsg, "提示", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                        {
                            foreach (Schema ylSchema in ylSchemas)
                            {
                                ylSchema.IsSelect = true;
                            }
                        }
                        else
                        {
                            //禁止启用
                            ckbSelect.IsChecked = false;
                        }
                    }
                }
                else
                {
                    //--去掉勾选，检查依赖该Schema的Schema
                    //获取所有依赖于或间接依赖于currentSchema的Schema
                    List<Schema> bylSchemas = new List<Schema>();
                    Tools.GetAllSchemaByl(_listSchemaList, bylSchemas, currentSchema);
                    //排除自身，有些情况下会出现自身
                    if (bylSchemas.IndexOf(currentSchema) > -1)
                    {
                        bylSchemas.Remove(currentSchema);
                    }
                    string strNoSelectBylSchema = "";
                    bool isAllNotSelect = true;//标识所有依赖于或间接依赖于currentSchema的Schema都没有勾选
                                               //遍历
                    foreach (Schema item in bylSchemas)
                    {
                        if (item.IsSelect)
                        {
                            isAllNotSelect = false;
                            strNoSelectBylSchema += item.SchemaId + " ";
                        }
                    }
                    if (!isAllNotSelect)
                    {
                        string strMsg = "已经启用的Schema:[" + strNoSelectBylSchema + "] 依赖于或间接依赖于该Schema：[" + currentSchema.SchemaId + "]，将全部停用。是否继续停用？";
                        if (MessageBox.Show(strMsg, "提示", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                        {
                            foreach (Schema item in bylSchemas)
                            {
                                item.IsSelect = false;
                            }
                        }
                        else
                        {
                            //禁止停用
                            ckbSelect.IsChecked = true;
                        }
                    }
                }
            }
        }


        /// <summary>
        /// DataGridSchema 选择改变事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataGridSchema_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataGridSchema.SelectedItems.Count > 0 && DataGridSchema.SelectedItems[0] is Schema selectedSchema)
            {
                string strText = string.Empty;
                strText += "schema_id： " + selectedSchema.SchemaId + Environment.NewLine;
                strText += "名称：  " + selectedSchema.Name + Environment.NewLine;
                strText += "作者：" + Environment.NewLine;
                strText += selectedSchema.Author;
                strText += Environment.NewLine + "描述：" + Environment.NewLine;
                strText += selectedSchema.Description;

                TbSchemaInfo.Text = strText;
            }
        }


        /// <summary>
        /// 获取更多输入方案
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnGetMoreSchema_Click(object sender, RoutedEventArgs e)
        {
            //调用 rime-install.bat
            //MessageBox.Show("该功能还在实现中。。。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            string rimeInstallBat = _strRootFolderPath + "\\rime-install.bat";
            if (File.Exists(rimeInstallBat))
            {
                try
                {
                    //启动外部程序
                    Process procRimeInstall = Process.Start(rimeInstallBat);
                    if (procRimeInstall != null)
                    {
                        //监视进程退出
                        procRimeInstall.EnableRaisingEvents = true;
                        //指定退出事件方法
                        procRimeInstall.Exited += ProcRimeInstall_Exited;
                    }
                }
                catch (ArgumentException ex)
                {
                    Console.Write(ex);
                    MessageBox.Show("rime-install.bat 调用失败，该功能无法使用!", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            else
            {
                MessageBox.Show("rime-install.bat 文件没有找到，该功能无法使用!", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// rime-install.bat 执行完成后回调
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ProcRimeInstall_Exited(object sender, EventArgs e)
        {
            if (MessageBox.Show("将重新载入方案列表，您未保存的修改可能丢失，是否继续？\r\n 【否】将在下次重新载入配置时刷新方案列表", "提示", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                //交由主线程执行
                Dispatcher.Invoke(() =>
                {
                    //载入方案信息
                    //LoadSchemas();
                    BackgroundWorker bwLoadSchemas = new BackgroundWorker();
                    bwLoadSchemas.DoWork += LoadSchemas;//执行的任务
                    bwLoadSchemas.RunWorkerCompleted += LoadSchemasCompleted;//任务执行完成后的回调
                    bwLoadSchemas.RunWorkerAsync();//开始执行后台任务
                });
            }
        }

        #endregion

        #region 备份部分 控件事件

        #region 备份部分的几个方法

        /// <summary>
        /// 备份文件后台进程
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BackupFileWorker(object sender, DoWorkEventArgs e)
        {
            //检查备份目录，没有就创建
            if (!Directory.Exists(_strBackupsFolder)) Directory.CreateDirectory(_strBackupsFolder);
            //构建备份文件的 路径+文件名
            string strDataTime = DateTime.Now.ToString("yyyyMMddHHmmssfff");
            string strArchiveFileName = _strBackupsFolder + strDataTime + ".zip";
            //开始备份
            _backupRestoreResult = PackagingUtil.PackageFolder(_strUserFolderPath, strArchiveFileName, true);
        }


        /// <summary>
        /// 还原文件后台进程
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RestoreFileWorker(object sender, DoWorkEventArgs e)
        {
            if (e.Argument != null)
            {
                string strFileName = (string)e.Argument;

                //检查备份目录，没有就创建
                if (!Directory.Exists(_strBackupsFolder)) Directory.CreateDirectory(_strBackupsFolder);

                strFileName = _strBackupsFolder + strFileName;//拼接还原文件
                //处理用户目录，解压到用户名上层目录
                //F:\Users\小狼毫配置 ---> F:\Users
                //string strDir = _strUserFolderPath.Substring(0, _strUserFolderPath.LastIndexOf("\\"));
                //开始还原--解压
                _backupRestoreResult = PackagingUtil.ExtractFile(_strUserFolderPath, strFileName, true);
            }

        }

        /// <summary>
        /// 关闭加载层
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CloseLP_Loading(object sender, RunWorkerCompletedEventArgs e)
        {
            //配置信息重新载入
            LoadBackInfo();
            //关闭加载层
            LpLoading.Visibility = Visibility.Collapsed;
            string msg = _isBackup ? "备份" : "还原";
            msg += _backupRestoreResult == 1 ? "成功" : "失败";
            if (!_isBackup && _backupRestoreResult == 1) msg += "。接下来将重新部署，请耐心等待...";
            //提示结果
            MessageBox.Show(msg, "提示", MessageBoxButton.OK);
            if (!_isBackup)
            {
                // 小狼毫重新部署并等待
                DeployAndWait();
            }
        }


        #endregion

        /// <summary>
        /// 备份按钮单击事件---执行备份
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            //提示
            MessageBox.Show("备份前请暂停使用小狼毫输入法", "提示", MessageBoxButton.OK, MessageBoxImage.Information);

            //显示加载层,设置提示文字
            _backupRestoreResult = 0;
            _isBackup = true;
            LpLoading.Visibility = Visibility.Visible;
            LpLoading.LoadingText = "备份中...";

            BackgroundWorker backupWorker = new BackgroundWorker();
            backupWorker.DoWork += BackupFileWorker;//执行的任务
            backupWorker.RunWorkerCompleted += CloseLP_Loading;//任务执行完成后的回调
            backupWorker.RunWorkerAsync();//开始执行后台任务
        }

        /// <summary>
        /// 还原按钮单击事件--执行还原操作
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnRestore_Click(object sender, RoutedEventArgs e)
        {
            if (DataGridBack.SelectedIndex > -1)
            {
                if (DataGridBack.SelectedItem is BackAndRestoreItems items)
                {
                    if (MessageBox.Show("还原该备份会覆盖当前的配置，是否继续？\n还原前请暂停使用小狼毫输入法", "提示", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        //显示加载层,设置提示文字
                        _backupRestoreResult = 0;
                        _isBackup = false;
                        LpLoading.Visibility = Visibility.Visible;
                        LpLoading.LoadingText = "还原中...";

                        BackgroundWorker restoreWorker = new BackgroundWorker();
                        restoreWorker.DoWork += RestoreFileWorker;//执行的任务
                        restoreWorker.RunWorkerCompleted += CloseLP_Loading;//任务执行完成后的回调
                        restoreWorker.RunWorkerAsync(items.ItemFileName);//开始执行后台任务
                        return;
                    }
                }
            }
            MessageBox.Show("请选择需要还原的一行", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        /// <summary>
        /// 删除备份
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnBackDelete_Click(object sender, RoutedEventArgs e)
        {
            if (DataGridBack.SelectedIndex > -1)
            {
                if (DataGridBack.SelectedItem is BackAndRestoreItems items)
                {
                    if (MessageBox.Show("确定要删除该备份吗？", "提示", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        string strFileName = _strBackupsFolder + items.ItemFileName;
                        try
                        {
                            File.Delete(strFileName);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }
                        //配置信息重新载入
                        LoadBackInfo();

                        return;
                    }
                }
            }
            MessageBox.Show("请选择需要删除的一行", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 重新载入备份信息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadBackInfo();//配置信息重新载入
        }

        /// <summary>
        /// 还原默认设置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnToDefault_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("还原默认设置将删除您的用户配置，让小狼毫重新部署生成默认的配置。删除前会自动备份您的配置，是否继续？", "提示", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                //备份数据
                BtnBack_Click(null, null);
                //删除配置目录
                try
                {
                    File.Delete(_strUserDefaultCustomPath);
                    File.Delete(_strUserWeaselCustomPathPath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }

                DeployAndWait();// 小狼毫重新部署并等待
                LoadBackInfo();//配置信息重新载入
            }
        }

        #endregion

        #region 关于页面 控件事件

        private void BtnGitHub_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/qzly/RimeControl");
        }

        #endregion


        /// <summary>
        /// 小狼毫重新部署并等待
        /// </summary>
        private void DeployAndWait()
        {
            //打开屏蔽提示层
            LpLoading.Visibility = Visibility.Visible;//显示
            LpLoading.LoadingText = "重新部署中...";

            //小狼毫重新部署
            Process procWeaselDeploy = Process.Start(_strRootFolderPath + "\\WeaselDeployer.exe", "/deploy");
            if (procWeaselDeploy != null)
            {
                //监视进程退出
                procWeaselDeploy.EnableRaisingEvents = true;
                //指定退出事件方法
                procWeaselDeploy.Exited += ProcWeaselDeploy_Exited;
            }
        }

        /// <summary>
        /// 小狼毫重新部署完成后重新载入配置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ProcWeaselDeploy_Exited(object sender, EventArgs e)
        {
            //交由主线程执行
            Dispatcher.Invoke(() =>
            {
                //重新载入配置
                ProfileLoad();
                SettingLoad();
                //隐藏加载层
                LpLoading.Visibility = Visibility.Collapsed;
            });
        }

        /// <summary>
        /// 关闭事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (MessageBox.Show("配置可能未保存\n是否退出？", "提示", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
            {
                e.Cancel = true;
            }
        }

        /// <summary>
        /// 打开最新版本下载目录
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnNewVersion_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/qzly/RimeControl/releases/latest");
        }
    }
}
