﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace PoeTradeSearch
{
    public partial class MainWindow : Window
    {
        private bool Setting()
        {
#if DEBUG
            string path = System.IO.Path.GetFullPath("_POE_Data\\");
#else
            string path = System.Reflection.Assembly.GetExecutingAssembly().Location;
            path = path.Remove(path.Length - 4) + "Data\\";
#endif
            FileStream fs = null;
            try
            {
                fs = new FileStream(path + "Config.txt", FileMode.Open);
                string configText = "";
                string configBackup = "";
                using (StreamReader reader = new StreamReader(fs))
                {
                    fs = null;
                    configText = reader.ReadToEnd();
                    configBackup = configText;
                }

                using (StreamWriter writer = new StreamWriter(path + "Config.txt", false, Encoding.UTF8))
                {
                    try
                    {
                        ConfigData configData = JsonConvert.DeserializeObject<ConfigData>(configText);
                        configText = JsonConvert.SerializeObject(configData, Formatting.Indented);
                        writer.Write(configText);
                    } catch (Exception ex)
                    {
                        writer.Write(configBackup);
                        MessageBox.Show(Application.Current.MainWindow, ex.Message, "Error");
                        
                        return false;
                    }
                    mConfigData = Json.Deserialize<ConfigData>(configText);
                }
                if (mConfigData.Options.SearchPriceCount > 80)
                    mConfigData.Options.SearchPriceCount = 80;
                //-----------------------------
                if (mCreateDatabase)
                {
                    File.Delete(path + "Bases.txt");
                    File.Delete(path + "Words.txt");
                    File.Delete(path + "Prophecies.txt"); ;
                    File.Delete(path + "Monsters.txt");
                    File.Delete(path + "Filters.txt");

                    if (!BaseDataUpdates(path) || !FilterDataUpdates(path))
                        throw new UnauthorizedAccessException("failed to create database");
                }

                fs = new FileStream(path + "Bases.txt", FileMode.Open);
                using (StreamReader reader = new StreamReader(fs))
                {
                    fs = null;
                    string json = reader.ReadToEnd();
                    BaseData data = Json.Deserialize<BaseData>(json);
                    mBaseDatas = new List<BaseResultData>();
                    mBaseDatas.AddRange(data.Result[0].Data);
                }

                fs = new FileStream(path + "Words.txt", FileMode.Open);
                using (StreamReader reader = new StreamReader(fs))
                {
                    fs = null;
                    string json = reader.ReadToEnd();
                    WordData data = Json.Deserialize<WordData>(json);
                    mWordDatas = new List<WordeResultData>();
                    mWordDatas.AddRange(data.Result[0].Data);
                }

                fs = new FileStream(path + "Prophecies.txt", FileMode.Open);
                using (StreamReader reader = new StreamReader(fs))
                {
                    fs = null;
                    string json = reader.ReadToEnd();
                    BaseData data = Json.Deserialize<BaseData>(json);
                    mProphecyDatas = new List<BaseResultData>();
                    mProphecyDatas.AddRange(data.Result[0].Data);
                }

                fs = new FileStream(path + "Monsters.txt", FileMode.Open);
                using (StreamReader reader = new StreamReader(fs))
                {
                    fs = null;
                    string json = reader.ReadToEnd();
                    BaseData data = Json.Deserialize<BaseData>(json);
                    mMonsterDatas = new List<BaseResultData>();
                    mMonsterDatas.AddRange(data.Result[0].Data);
                }

                fs = new FileStream(path + "Filters.txt", FileMode.Open);
                using (StreamReader reader = new StreamReader(fs))
                {
                    fs = null;
                    string json = reader.ReadToEnd();
                    mFilterData = Json.Deserialize<FilterData>(json);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(Application.Current.MainWindow, ex.Message, "Error");
                return false;
            }
            finally
            {
                if (fs != null)
                    fs.Dispose();
            }

            return true;
        }

        private void ForegroundMessage(string message, string caption, MessageBoxButton button, MessageBoxImage icon)
        {
            //MessageBox.Show(Application.Current.MainWindow, message, caption, button, icon);
            Native.SetForegroundWindow(Native.FindWindow(Restr.PoeClass, Restr.PoeCaption));
        }

        private void SetSearchButtonText()
        {
            bool isExchange = bdExchange.Visibility == Visibility.Visible && (cbOrbs.SelectedIndex > 0 || cbSplinters.SelectedIndex > 0);
            btnSearch.Content = "Open in browser (pathofexile.com)";
        }

        private void ResetControls()
        {
            tbLinksMin.Text = "";
            tbSocketMin.Text = "";
            tbLinksMax.Text = "";
            tbSocketMax.Text = "";
            tbLvMin.Text = "";
            tbLvMax.Text = "";
            tbQualityMin.Text = "";
            tbQualityMax.Text = "";
            tkDetail.Text = "";

            cbAiiCheck.IsChecked = false;
            ckLv.IsChecked = false;
            ckQuality.IsChecked = false;
            ckSocket.IsChecked = false;
            cbInfluence1.SelectedIndex = 0;
            cbInfluence2.SelectedIndex = 0;
            cbCorrupt.SelectedIndex = mConfigData.Options.AutoSelectCorrupt == "no" ? 2 : (mConfigData.Options.AutoSelectCorrupt == "yes" ? 1 : 0);
            cbCorrupt.BorderThickness = new Thickness(1);

            cbOrbs.SelectionChanged -= CbOrbs_SelectionChanged;
            cbSplinters.SelectionChanged -= CbOrbs_SelectionChanged;
            cbOrbs.SelectedIndex = 0;
            cbSplinters.SelectedIndex = 0;
            cbOrbs.SelectionChanged += CbOrbs_SelectionChanged;
            cbSplinters.SelectionChanged += CbOrbs_SelectionChanged;

            cbOrbs.FontWeight = FontWeights.Normal;
            cbSplinters.FontWeight = FontWeights.Normal;

            lbDPS.Content = "Options";
            lbBuyPay.Content = "";
            SetSearchButtonText();

            Topmost = mConfigData.Options.AlwaysOnTop;
            
            ckLv.Content = "iLevel";
            Synthesis.Content = "Synthesis";
            lbSocketBackground.Visibility = Visibility.Hidden;

            ckSocket.Visibility = Visibility.Visible;
            tbSocketMin.Visibility = Visibility.Visible;
            tbSocketMax.Visibility = Visibility.Visible;
            tbLinksMin.Visibility = Visibility.Visible;
            tbLinksMax.Visibility = Visibility.Visible;
            lbAmp.Visibility = Visibility.Visible;

            cbElderGuardian.SelectedIndex = 0;
            cbMapInfluence.SelectedIndex = 0;
            cbElderGuardian.Visibility = Visibility.Hidden;
            cbMapInfluence.Visibility = Visibility.Hidden;

            poePriceTab.IsEnabled = false;
            liPoePriceInfo.Items.Clear();

            cbRarity.Items.Clear();
            cbRarity.Items.Add(Restr.All);
            cbRarity.Items.Add(Restr.Normal);
            cbRarity.Items.Add(Restr.Magic);
            cbRarity.Items.Add(Restr.Rare);
            cbRarity.Items.Add(Restr.Unique);

            cbAccountState.Items.Clear();
            cbAccountState.Items.Add("online");
            cbAccountState.Items.Add("any");
            cbAccountState.SelectedIndex = 0;

            tbPriceMinStock.Text = "1";
            tbPriceMinStock.IsEnabled = false;

            tabControl1.SelectedIndex = 0;
            cbPriceListCount.SelectedIndex = (int)Math.Ceiling(mConfigData.Options.SearchPriceCount / 20) - 1;
            tbPriceFilterMin.Text = mConfigData.Options.SearchPriceMin > 0 ? mConfigData.Options.SearchPriceMin.ToString() : "";

            for (int i = 0; i < 10; i++)
            {
                ((CheckBox)this.FindName("tbOpt" + i + "_2")).IsChecked = false;
                ((CheckBox)this.FindName("tbOpt" + i + "_3")).IsChecked = false;
                ((TextBox)this.FindName("tbOpt" + i)).Text = "";
                ((TextBox)this.FindName("tbOpt" + i)).Background = SystemColors.WindowBrush;
                ((TextBox)this.FindName("tbOpt" + i + "_0")).Text = "";
                ((TextBox)this.FindName("tbOpt" + i + "_1")).Text = "";
                ((CheckBox)this.FindName("tbOpt" + i + "_2")).IsEnabled = true;
                ((CheckBox)this.FindName("tbOpt" + i + "_3")).Visibility = Visibility.Hidden;
                ((TextBox)this.FindName("tbOpt" + i)).BorderBrush = SystemColors.ActiveBorderBrush;
                ((TextBox)this.FindName("tbOpt" + i + "_0")).BorderBrush = SystemColors.ActiveBorderBrush;
                ((TextBox)this.FindName("tbOpt" + i + "_1")).BorderBrush = SystemColors.ActiveBorderBrush;
                ((CheckBox)this.FindName("tbOpt" + i + "_2")).BorderBrush = SystemColors.ActiveBorderBrush;
                ((CheckBox)this.FindName("tbOpt" + i + "_3")).BorderBrush = SystemColors.ActiveBorderBrush;

                ((Label)this.FindName("lbOpt" + i)).Content = "";

                ((ComboBox)this.FindName("cbOpt" + i)).Items.Clear();
                // ((ComboBox)this.FindName("cbOpt" + i)).ItemsSource = new List<FilterEntrie>();
                ((ComboBox)this.FindName("cbOpt" + i)).DisplayMemberPath = "Name";
                ((ComboBox)this.FindName("cbOpt" + i)).SelectedValuePath = "Name";
            }
        }
        private StringDictionary metamorphMods = new StringDictionary();
        private void ItemTextParser(string itemText, bool isWinShow = true)
        {
            Restr.ModText = itemText;
            string itemName = "";
            string itemType = "";
            string itemRarity = "";
            string itemInherits = "";
            string itemID = "";
            metamorphMods.Clear();
            try
            {
                string[] asData = (itemText ?? "").Trim().Split(new string[] { "--------" }, StringSplitOptions.None);

                if (asData.Length > 1 && asData[0].IndexOf(Restr.Rarity + ": ") == 0)
                {
                    ResetControls();
                    mItemBaseName = new ItemBaseName();

                    string[] asOpt = asData[0].Trim().Split(new string[] { "\r\n" }, StringSplitOptions.None);

                    itemRarity = asOpt[0].Split(':')[1].Trim();
                    itemName = Regex.Replace(asOpt[1] ?? "", @"<<set:[A-Z]+>>", "");
                    itemType = asOpt.Length > 2 && asOpt[2] != "" ? Regex.Replace(asOpt[2] ?? "", @"<<set:[A-Z]+>>", "") : itemName;
                    if (asOpt.Length == 2) itemName = "";

                    bool is_flask = false, is_prophecy = false, is_map_fragment = false, is_met_entrails = false, is_captured_beast = false, is_elder_map = false;

                    int k = 0, baki = 0, notImpCnt = 0;
                    double attackSpeedIncr = 0;
                    double PhysicalDamageIncr = 0;
                    List<Itemfilter> itemfilters = new List<Itemfilter>();

                    Dictionary<string, string> lItemOption = new Dictionary<string, string>()
                    {
                        { Restr.Quality, "" }, { Restr.Lv, "" }, { Restr.ItemLv, "" }, { Restr.CharmLv, "" }, { Restr.MaTier, "" }, { Restr.Socket, "" },
                        { Restr.PhysicalDamage, "" }, { Restr.ElementalDamage, "" }, { Restr.ChaosDamage, "" }, { Restr.AttacksPerSecond, "" },
                        { Restr.Shaper, "" }, { Restr.Elder, "" }, { Restr.Crusader, "" }, { Restr.Redeemer, "" }, { Restr.Hunter, "" }, { Restr.Warlord, "" },
                        { Restr.Synthesis, "" }, { Restr.Corrupt, "" }, { Restr.Unidentify, "" }, { Restr.Vaal, "" }
                    };

                  
                    for (int i = 1; i < asData.Length; i++)
                    {
                        asOpt = asData[i].Trim().Split(new string[] { "\r\n" }, StringSplitOptions.None);

                        for (int j = 0; j < asOpt.Length; j++)
                        {
                            if (asOpt[j].Trim() == "") continue;


                            string[] asTmp = asOpt[j].Split(':');

                            if (asOpt[j].Contains("Cannot be Frozen"))
                            {
                                asOpt[j] = "100% chance to Avoid being Frozen";
                            }

                            if (lItemOption.ContainsKey(asTmp[0]))
                            {
                                if (lItemOption[asTmp[0]] == "")
                                    lItemOption[asTmp[0]] = asTmp.Length > 1 ? asTmp[1] : "_TRUE_";
                            }
                            else
                            {
                                
                                if (itemRarity == Restr.Gem && (Restr.Vaal + " " + itemType) == asTmp[0])
                                    lItemOption[Restr.Vaal] = "_TRUE_";
                                else if (!is_prophecy && asTmp[0].IndexOf(Restr.ChkProphecy) == 0)
                                    is_prophecy = true;
                                else if (!is_map_fragment && asTmp[0].IndexOf(Restr.ChkMapFragment) == 0)
                                    is_map_fragment = true;
                                else if (!is_met_entrails && asTmp[0].IndexOf(Restr.ChkMetEntrails) == 0)
                                    is_met_entrails = true;
                                else if (!is_flask && asTmp.Length > 1 && asTmp[0] == Restr.ChkFlask)
                                    is_flask = true;
                                else if (!is_captured_beast && asTmp[0] == Restr.ChkBeast1)
                                {
                                    string[] asTmp22 = asOpt[j + 1].Split(':');
                                    is_captured_beast = asTmp22.Length > 1 && asTmp22[0] == Restr.ChkBeast2;
                                }
                                else if (lItemOption[Restr.ItemLv] != "" && k < 10)
                                {
                                    double min = 99999, max = 99999;
                                    int option = 99999;
                                    bool resistance = false;
                                    bool crafted = asOpt[j].IndexOf("(crafted)") > -1;

                                    string input = Regex.Replace(asOpt[j], @" \([a-zA-Z]+\)", "");
                                    input = Regex.Escape(Regex.Replace(input, @"[+-]?[0-9]+\.[0-9]+|[+-]?[0-9]+", "#"));
                                    input = Regex.Replace(input, @"\\#", "[+-]?([0-9]+\\.[0-9]+|[0-9]+|\\#)");
                                    input = input + (is_captured_beast ? "\\(" + Restr.Captured + "\\)" : "");

                                    FilterResultEntrie filter = null;
                                    Regex rgx = new Regex("^" + input + "$", RegexOptions.IgnoreCase);
                                    
                                    foreach (FilterResult filterResult in mFilterData.Result)
                                    {
                                        FilterResultEntrie[] entries = Array.FindAll(filterResult.Entries, x => rgx.IsMatch(x.Text));
                                        if (entries.Length > 0)
                                        {
                                            MatchCollection matches1 = Regex.Matches(asOpt[j], @"[-]?[0-9]+\.[0-9]+|[-]?[0-9]+");
                                            foreach (FilterResultEntrie entrie in entries)
                                            {
                                                // 장비 옵션 (특정) 이 겹칠경우 (특정) 대신 일반 옵션 값 사용 (후에 json 만들때 다시 검사함)

                                                if (entries.Length > 1 && entrie.Part != null)
                                                    continue;

                                                if (entrie.Type == "monster")
                                                {
                                                    if (!metamorphMods.ContainsKey(entrie.Text.Trim()))
                                                    {
                                                        metamorphMods.Add(entrie.Text.Trim(), "1");
                                                    }
                                                    else
                                                    {
                                                        string val = metamorphMods[entrie.Text.Trim()];
                                                        int num = (int.Parse(val) + 1);
                                                        metamorphMods[entrie.Text.Trim()] = (num).ToString();
                                                        continue;
                                                    }
                                                }

                                                int idxMin = 0, idxMax = 0;
                                                bool isMin = false, isMax = false;
                                                bool isBreak = true;


                                                MatchCollection matches2 = Regex.Matches(entrie.Text, @"[-]?[0-9]+\.[0-9]+|[-]?[0-9]+|#");

                                                for (int t = 0; t < matches2.Count; t++)
                                                {
                                                    if (matches2[t].Value == "#")
                                                    {
                                                        if (!isMin)
                                                        {
                                                            isMin = true;
                                                            idxMin = t;
                                                        }
                                                        else if (!isMax)
                                                        {
                                                            isMax = true;
                                                            idxMax = t;
                                                        }
                                                    }
                                                    else if (matches1[t].Value != matches2[t].Value)
                                                    {
                                                        isBreak = false;
                                                        break;
                                                    }
                                                }

                                                if (isBreak)
                                                {

                                                    ((ComboBox)this.FindName("cbOpt" + k)).Items.Add(new FilterEntrie(entrie.ID, filterResult.Label));
                                                    if (filter == null)
                                                    {

                                                        string[] id_split = entrie.ID.Split('.');
                                                        resistance = id_split.Length == 2 && Restr.lResistance.ContainsKey(id_split[1]);

                                                        filter = entrie;
                                                        MatchCollection matches = Regex.Matches(asOpt[j], @"[-]?[0-9]+\.[0-9]+|[-]?[0-9]+");
                                                        min = isMin && matches.Count > idxMin ? StrToDouble(((Match)matches[idxMin]).Value, 99999) : 99999;
                                                        max = isMax && idxMin < idxMax && matches.Count > idxMax ? StrToDouble(((Match)matches[idxMax]).Value, 99999) : 99999;
                                                    }

                                                    break;
                                                }
                                            }
                                        } else
                                        {
                                            FilterResultEntrie entrie = null;
                                            if (asOpt[j].Contains("Allocates "))
                                                entrie = Array.Find(filterResult.Entries, x => x.Text.Contains("Allocates #"));
                                            else if (asOpt[j].Contains("occupied by"))
                                            {
                                                entrie = Array.Find(filterResult.Entries, x => x.Text.Contains("Map is occupied by #"));
                                                cbMapInfluence.SelectedIndex = 2;
                                                min = 99999;
                                                max = 99999;
                                                is_elder_map = true;
                                            }
                                            else if (asOpt[j].Contains("influenced by"))
                                            {
                                                entrie = Array.Find(filterResult.Entries, x => x.Text.Contains("Area is influenced by #"));
                                                min = 99999;
                                                max = 99999;
                                                is_elder_map = true;
                                                cbMapInfluence.SelectedIndex = 1;
                                            }
                                            else if (asOpt[j].Contains("Affects Passives in "))
                                                entrie = Array.Find(filterResult.Entries, x => x.Text.Contains("Affects Passives in #"));

                                            if (entrie != null && entrie.Option != null)
                                            {
                                                ((ComboBox)this.FindName("cbOpt" + k)).Items.Add(new FilterEntrie(entrie.ID, filterResult.Label));
                                                filter = entrie;
                                                foreach (FilterResultOptions fro in entrie.Option.Options)
                                                {
                                                    if (asOpt[j].Contains(fro.Text))
                                                    {
                                                        Console.WriteLine(fro.ID);
                                                        option = fro.ID;
                                                        break;
                                                    }
                                                }
                                                if (is_elder_map)
                                                    cbElderGuardian.SelectedIndex = option;
                                                
                                            }
                                        }
                                    }

                                    if (filter != null)
                                    {
                                        ((ComboBox)this.FindName("cbOpt" + k)).SelectedValue = Restr.Crafted;
                                        int selidx = ((ComboBox)this.FindName("cbOpt" + k)).SelectedIndex;

                                        if (crafted && selidx > -1)
                                        {
                                            ((TextBox)this.FindName("tbOpt" + k)).BorderBrush = System.Windows.Media.Brushes.Blue;
                                            ((TextBox)this.FindName("tbOpt" + k + "_0")).BorderBrush = System.Windows.Media.Brushes.Blue;
                                            ((TextBox)this.FindName("tbOpt" + k + "_1")).BorderBrush = System.Windows.Media.Brushes.Blue;
                                            ((CheckBox)this.FindName("tbOpt" + k + "_2")).BorderBrush = System.Windows.Media.Brushes.Blue;
                                            ((CheckBox)this.FindName("tbOpt" + k + "_3")).BorderBrush = System.Windows.Media.Brushes.Blue;
                                            ((ComboBox)this.FindName("cbOpt" + k)).SelectedIndex = selidx;
                                        }
                                        else
                                        {
                                            ((ComboBox)this.FindName("cbOpt" + k)).SelectedValue = Restr.Pseudo;
                                            selidx = ((ComboBox)this.FindName("cbOpt" + k)).SelectedIndex;

                                            if (selidx == -1 && ((ComboBox)this.FindName("cbOpt" + k)).Items.Count > 0)
                                            {
                                                FilterEntrie filterEntrie = (FilterEntrie)((ComboBox)this.FindName("cbOpt" + k)).Items[0];
                                                string[] id_split = filterEntrie.ID.Split('.');
                                                if (id_split.Length == 2 && Restr.lPseudo.ContainsKey(id_split[1]))
                                                {
                                                    ((ComboBox)this.FindName("cbOpt" + k)).Items.Add(new FilterEntrie("pseudo." + Restr.lPseudo[id_split[1]], Restr.Pseudo));
                                                }
                                            }

                                            selidx = -1;

                                            if (is_captured_beast)
                                            {
                                                ((ComboBox)this.FindName("cbOpt" + k)).SelectedValue = Restr.Monster;
                                                selidx = ((ComboBox)this.FindName("cbOpt" + k)).SelectedIndex;
                                            }
                                            else if (mConfigData.Options.AutoSelectPseudo)
                                            {
                                                ((ComboBox)this.FindName("cbOpt" + k)).SelectedValue = Restr.Pseudo;
                                                selidx = ((ComboBox)this.FindName("cbOpt" + k)).SelectedIndex;
                                            }

                                            if (selidx == -1)
                                            {
                                                ((ComboBox)this.FindName("cbOpt" + k)).SelectedValue = Restr.Explicit;
                                                selidx = ((ComboBox)this.FindName("cbOpt" + k)).SelectedIndex;
                                            }

                                            if (selidx == -1)
                                            {
                                                ((ComboBox)this.FindName("cbOpt" + k)).SelectedValue = Restr.Fractured;
                                                selidx = ((ComboBox)this.FindName("cbOpt" + k)).SelectedIndex;
                                            }

                                            if (selidx == -1 && ((ComboBox)this.FindName("cbOpt" + k)).Items.Count == 1)
                                            {
                                                selidx = 0;
                                            }

                                            ((ComboBox)this.FindName("cbOpt" + k)).SelectedIndex = selidx;
                                        }

                                        if (i != baki)
                                        {
                                            baki = i;
                                            notImpCnt = 0;
                                        }

                                        ((TextBox)this.FindName("tbOpt" + k)).Text = filter.Text;
                                        ((CheckBox)this.FindName("tbOpt" + k + "_3")).Visibility = resistance ? Visibility.Visible : Visibility.Hidden;

                                        if (min != 99999 && max != 99999)
                                        {
                                            if (filter.Text.Contains("# to #"))
                                            {
                                                min += max;
                                                min = Math.Truncate(min / 2 * 10) / 10;
                                                max = 99999;
                                            }
                                        }
                                        else if (min != 99999 || max != 99999)
                                        {
                                            string[] split = filter.ID.Split('.');
                                            bool defMaxPosition = split.Length == 2 && Restr.lDefaultPosition.ContainsKey(split[1]);
                                            if ((defMaxPosition && min > 0 && max == 99999) || (!defMaxPosition && min < 0 && max == 99999))
                                            {
                                                max = min;
                                                min = 99999;
                                            }
                                        }

                                        /*if (filter.Text.Contains(Restr.FlatPhysicalDamage) ||
                                                filter.Text.Contains(Restr.FlatColdDamage) ||
                                                filter.Text.Contains(Restr.FlatLightningDamage) ||
                                                filter.Text.Contains(Restr.FlatFireDamage) ||
                                                filter.Text.Contains(Restr.FlatChaosDamage))
                                        {
                                            min = (min + max) / 2;
                                            max = 99999;
                                        }*/

                                        ((Label)this.FindName("lbOpt" + k)).Content = min == 99999 ? "" : min.ToString();

                                        if (filter.Type != Restr.Enchant && filter.Type != Restr.Implicit)
                                        {
                                            if (itemRarity == Restr.Unique)
                                            {
                                                if (min != 99999)
                                                {
                                                    min = min * (mConfigData.Options.UniqueMinValuePercent / 100);
                                                    if (mConfigData.Options.SetMaxValue)
                                                        max = min * (mConfigData.Options.UniqueMaxValuePercent / 100);
                                                }
                                            }
                                            else
                                            {
                                                if (min != 99999)
                                                {
                                                    min = min * (mConfigData.Options.MinValuePercent / 100);
                                                    if (mConfigData.Options.SetMaxValue)
                                                        max = min * (mConfigData.Options.MaxValuePercent / 100);
                                                }
                                            }
                                        }

                                        if (option != 99999 && !is_elder_map)
                                            min = option;

                                        ((TextBox)this.FindName("tbOpt" + k + "_0")).Text = min == 99999 ? "" : min.ToString();
                                        ((TextBox)this.FindName("tbOpt" + k + "_1")).Text = max == 99999 ? "" : max.ToString();
                                        

                                        Itemfilter itemfilter = new Itemfilter
                                        {
                                            id = filter.Type,
                                            text = filter.Text,
                                            option = option,
                                            max = max,
                                            min = min,
                                            disabled = true
                                        };

                                        itemfilters.Add(itemfilter);

                                        if (filter.Text == Restr.AttackSpeedIncr && min > 0 && min < 999)
                                        {
                                            attackSpeedIncr += min;
                                        }
                                        else if (filter.Text == Restr.PhysicalDamageIncr && min > 0 && min < 9999)
                                        {
                                            PhysicalDamageIncr += min;
                                        }

                                        k++;
                                        notImpCnt++;
                                    }
                                }
                            }
                        }
                    }
                    if (lItemOption[Restr.Socket] != "")
                    {
                        string socket = lItemOption[Restr.Socket];
                        int sckcnt = socket.Replace(" ", "-").Split('-').Length - 1;
                        string[] scklinks = socket.Split(' ');

                        int lnkcnt = 0;
                        for (int s = 0; s < scklinks.Length; s++)
                        {
                            if (lnkcnt < scklinks[s].Length)
                                lnkcnt = scklinks[s].Length;
                        }

                        int link = lnkcnt < 3 ? 0 : lnkcnt - (int)Math.Ceiling((double)lnkcnt / 2) + 1;
                        tbSocketMin.Text = sckcnt.ToString();
                        tbLinksMin.Text = link > 0 ? link.ToString() : "";
                        ckSocket.IsChecked = link > 4;
                    }
                    
                    //check if item is corrupted
                    bool is_corrupted = lItemOption[Restr.Corrupt] == "_TRUE_";
                    bool is_blight = false;
                    bool is_unIdentify = lItemOption[Restr.Unidentify] == "_TRUE_";
                    bool is_map = lItemOption[Restr.MaTier] != "";
                    bool is_gem = itemRarity == Restr.Gem;
                    bool is_currency = itemRarity == Restr.Currency;
                    bool is_divinationCard = itemRarity == Restr.DivinationCard;

                    //if no option is selected in config, use item corruption status. Otherwise, use the config value
                    if (!is_divinationCard)
                        cbCorrupt.SelectedIndex = (int)cbCorrupt.SelectedIndex == 0 ? (is_corrupted ? 1 : 2) : cbCorrupt.SelectedIndex;
                    else
                        cbCorrupt.SelectedIndex = 0;

                    if (is_map || is_currency) is_map_fragment = false;
                    bool is_detail = is_gem || is_currency || is_divinationCard || is_prophecy || is_map_fragment;


                    if (itemRarity == Restr.Rare && !is_map)
                        poePriceTab.IsEnabled = true;

                    if (is_elder_map)
                    {
                        ckSocket.Visibility = Visibility.Hidden;
                        tbSocketMin.Visibility = Visibility.Hidden;
                        tbSocketMax.Visibility = Visibility.Hidden;
                        tbLinksMin.Visibility = Visibility.Hidden;
                        tbLinksMax.Visibility = Visibility.Hidden;
                        lbAmp.Visibility = Visibility.Hidden;

                        cbElderGuardian.Visibility = Visibility.Visible;
                        cbMapInfluence.Visibility = Visibility.Visible;
                    }

                    if (is_met_entrails)
                    {
                        itemID = itemInherits = "Entrailles/Entrails";
                        string[] tmp = itemType.Split(' ');
                        itemType = "Metamorph " + tmp[tmp.Length - 1];
                    }
                    else if (is_prophecy)
                    {
                        itemRarity = Restr.Prophecy;
                        BaseResultData tmpBaseType = mProphecyDatas.Find(x => x.NameEn == itemType);
                        if (tmpBaseType != null)
                        {
                            itemID = tmpBaseType.ID;
                            itemInherits = tmpBaseType.InheritsFrom;
                        }
                    }
                    else if (is_captured_beast)
                    {
                        BaseResultData tmpBaseType = mMonsterDatas.Find(x => x.NameEn == itemType);
                        if (tmpBaseType != null)
                        {
                            itemID = tmpBaseType.ID;
                            itemInherits = tmpBaseType.InheritsFrom;
                        }
                    }
                    else
                    {
                        if (is_gem && lItemOption[Restr.Corrupt] == "_TRUE_" && lItemOption[Restr.Vaal] == "_TRUE_")
                        {
                            BaseResultData tmpBaseType = mBaseDatas.Find(x => x.NameEn == Restr.Vaal + " " + itemType);
                            if (tmpBaseType != null)
                                itemType = tmpBaseType.NameEn;
                        }

                        if (!is_unIdentify && itemRarity == Restr.Magic)
                        {
                            itemType = itemType.Split(new string[] { " of " }, StringSplitOptions.None)[0].Trim();
                        }

                        if ((is_unIdentify || itemRarity == Restr.Normal) && itemType.Length > 4 && itemType.IndexOf(Restr.Higher + " ") == 0)
                            itemType = itemType.Substring(9);

                        if (is_map && itemType.Length > 5)
                        {
                            if (itemType.IndexOf(Restr.Blighted + " ") == 0)
                            {
                                is_blight = true;
                                itemType = itemType.Substring(9);
                            }

                            if (itemType.Substring(0, 7) == Restr.Shaped + " ")
                                itemType = itemType.Substring(7);
                        }
                        else if (lItemOption[Restr.Synthesis] == "_TRUE_")
                        {
                            if (itemType.Substring(0, 12) == Restr.Synthesised + " ")
                                itemType = itemType.Substring(12);
                        }

                        /*if (!is_unIdentify && itemRarity == Restr.Magic)
                        {
                            string[] tmp = itemType.Split(' ');
                            if (tmp.Length > 1)
                            {
                                string tmpName = "";
                                for (int i = 1; i < tmp.Length; i++)
                                {
                                    tmpName = tmp[i - 1] + " " + tmp[i];
                                    BaseResultData tmpBaseType = mBaseDatas.Find(x => x.NameEn.Contains(tmpName));
                                    if (tmpBaseType != null)
                                    {
                                        itemType = tmpBaseType.NameEn;
                                        itemID = tmpBaseType.ID;
                                        itemInherits = tmpBaseType.InheritsFrom;
                                        break;
                                    }
                                }
                                
                            }
                        }*/
                        if (!is_unIdentify && itemRarity == Restr.Magic)
                        {
                            string[] tmp = itemType.Split(' ');
                            if (tmp.Length > 1)
                            {
                                for (int i = 0; i < tmp.Length - 1; i++)
                                {
                                    tmp[i] = "";
                                    string tmp2 = string.Join(" ", tmp).Trim();
                                    BaseResultData tmpBaseType = mBaseDatas.Find(x => x.NameEn == tmp2);
                                    if (tmpBaseType != null)
                                    {
                                        itemType = tmpBaseType.NameEn;
                                        itemID = tmpBaseType.ID;
                                        itemInherits = tmpBaseType.InheritsFrom;
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    if (itemInherits == "")
                    {
                        BaseResultData tmpBaseType = mBaseDatas.Find(x => x.NameEn == itemType);
                        if (tmpBaseType != null)
                        {
                            itemID = tmpBaseType.ID;
                            itemInherits = tmpBaseType.InheritsFrom;
                        }
                    }

                    mItemBaseName.Inherits = itemInherits.Split('/');

                    string item_quality = Regex.Replace(lItemOption[Restr.Quality].Trim(), "[^0-9]", "");

                    string inherit = mItemBaseName.Inherits[0];
                    string sub_inherit = mItemBaseName.Inherits.Length > 1 ? mItemBaseName.Inherits[1] : "";

                    bool is_essences = inherit == "Currency" && itemID.IndexOf("Currency/CurrencyEssence") == 0;
                    bool is_incubations = inherit == "Legion" && sub_inherit == "Incubator";

                    bool by_type = inherit == "Weapons" || inherit == "Quivers" || inherit == "Armours" || inherit == "Amulets" || inherit == "Rings" || inherit == "Belts";

                    is_detail = is_detail || is_incubations || (!is_detail && (inherit == "MapFragments" || inherit == "UniqueFragments" || inherit == "Labyrinth"));

                    if (is_detail)
                    {
                        mItemBaseName.NameEN = "";

                        try
                        {
                            
                            BaseResultData tmpBaseType = is_prophecy ? mProphecyDatas.Find(x => x.NameEn == itemType) : mBaseDatas.Find(x => x.NameEn == itemType);

                            mItemBaseName.TypeEN = tmpBaseType == null ? itemType : tmpBaseType.NameEn;

                            if (inherit == "Gems" || is_essences || is_incubations || inherit == "UniqueFragments" || inherit == "Labyrinth")
                            {
                                int i = inherit == "Gems" ? 3 : 1;
                                tkDetail.Text = asData.Length > 2 ? ((inherit == "Gems" || inherit == "Labyrinth" ? asData[i] : "") + asData[i + 1]) : "";
                            }
                            else
                            {
                                if ((tmpBaseType?.Detail ?? "") != "")
                                    tkDetail.Text = "Detail:" + '\n' + '\n' + tmpBaseType.Detail.Replace("\\n", "" + '\n');
                                else
                                {
                                    int i = inherit == "Delve" ? 3 : (is_divinationCard || inherit == "Currency" ? 2 : 1);

                                    tkDetail.Text = asData.Length > (i + 1) ? asData[i] + asData[i + 1] : asData[asData.Length - 1];

                                    if (asData.Length > (i + 1))
                                    {
                                        int v = asData[i - 1].TrimStart().IndexOf("Apply: ");
                                        tkDetail.Text += v > -1 ? "" + '\n' + '\n' + (asData[i - 1].TrimStart().Split('\n')[v == 0 ? 0 : 1].TrimEnd()) : "";
                                    }
                                }
                            }

                            if (is_gem)
                                tkDetail.Text = "Quality: " + Restr.lGemQualityProperties[itemType] + "\r\n" + tkDetail.Text;

                            tkDetail.Text = tkDetail.Text.Replace(Restr.SClickSplitItem, "");
                            tkDetail.Text = Regex.Replace(tkDetail.Text, "<(uniqueitem|prophecy|divination|gemitem|magicitem|rareitem|whiteitem|corrupted|default|normal|augmented|size:[0-9]+)>", "");

                            
                        }
                        catch { }
                    }
                    else
                    {
                        int Imp_cnt = itemfilters.Count - ((itemRarity == Restr.Normal || is_unIdentify) ? 0 : notImpCnt);

                        for (int i = 0; i < itemfilters.Count; i++)
                        {
                            Itemfilter ifilter = itemfilters[i];

                            if (i < Imp_cnt)
                            {
                                ((TextBox)this.FindName("tbOpt" + i)).BorderBrush = System.Windows.Media.Brushes.DarkRed;
                                ((TextBox)this.FindName("tbOpt" + i + "_0")).BorderBrush = System.Windows.Media.Brushes.DarkRed;
                                ((TextBox)this.FindName("tbOpt" + i + "_1")).BorderBrush = System.Windows.Media.Brushes.DarkRed;
                                ((CheckBox)this.FindName("tbOpt" + i + "_2")).BorderBrush = System.Windows.Media.Brushes.DarkRed;
                                ((CheckBox)this.FindName("tbOpt" + i + "_2")).IsChecked = false;
                                ((CheckBox)this.FindName("tbOpt" + i + "_3")).BorderBrush = System.Windows.Media.Brushes.DarkRed;

                                itemfilters[i].disabled = true;

                                ((ComboBox)this.FindName("cbOpt" + i)).SelectedValue = Restr.Enchant;

                                if (((ComboBox)this.FindName("cbOpt" + i)).SelectedIndex == -1)
                                {
                                    ((ComboBox)this.FindName("cbOpt" + i)).SelectedValue = Restr.Implicit;
                                }
                                if (ifilter.text.Contains("Area is influenced by") || ifilter.text.Contains("Map is occupied by"))
                                {
                                    itemfilters[i].disabled = false;
                                    ((CheckBox)this.FindName("tbOpt" + i + "_2")).IsChecked = true;
                                }
                            }
                            if (inherit != "")
                            {
                                string modType = (string)((ComboBox)this.FindName("cbOpt" + i)).SelectedValue;
                                foreach (ConfigChecked checkedMods in mConfigData.Checked)
                                {
                                    if (checkedMods.ModType != null)
                                    {
                                        string[] configModType = checkedMods.ModType.Split('/');
                                        foreach (string cmt in configModType)
                                        {
                                            if (checkedMods.ModType.ToLower() != "all")
                                            {
                                                if (cmt == modType.ToLower() && checkedMods.Text == ifilter.text)
                                                {
                                                    ((CheckBox)this.FindName("tbOpt" + i + "_2")).IsChecked = true;
                                                    itemfilters[i].disabled = false;
                                                }
                                            } else
                                            {
                                                if (checkedMods.Text == ifilter.text)
                                                {
                                                    ((CheckBox)this.FindName("tbOpt" + i + "_2")).IsChecked = true;
                                                    itemfilters[i].disabled = false;
                                                }
                                            }
                                        }
                                    }
                                }
                                if ((mConfigData.Options.AutoCheckUnique && itemRarity == Restr.Unique))
                                {
                                    ((CheckBox)this.FindName("tbOpt" + i + "_2")).IsChecked = true;
                                    itemfilters[i].disabled = false;
                                }
                                if ((Array.Find(mConfigData.DangerousMods, x => x.Text == ifilter.text && x.ID.IndexOf(inherit + "/") > -1) != null))
                                {
                                    ((TextBox)this.FindName("tbOpt" + i)).Background = System.Windows.Media.Brushes.Red;
                                }
                            }
                        }

                        // DPS 계산 POE-TradeMacro 참고
                        if (!is_unIdentify && inherit == "Weapons")
                        {
                            double PhysicalDPS = DamageToDPS(lItemOption[Restr.PhysicalDamage]);
                            double ElementalDPS = DamageToDPS(lItemOption[Restr.ElementalDamage]);
                            double ChaosDPS = DamageToDPS(lItemOption[Restr.ChaosDamage]);

                            double quality20Dps = item_quality == "" ? 0 : StrToDouble(item_quality, 0);
                            double attacksPerSecond = StrToDouble(Regex.Replace(lItemOption[Restr.AttacksPerSecond], @"\([a-zA-Z]+\)", "").Trim(), 0);

                            if (attackSpeedIncr > 0)
                            {
                                double baseAttackSpeed = attacksPerSecond / (attackSpeedIncr / 100 + 1);
                                double modVal = baseAttackSpeed % 0.05;
                                baseAttackSpeed += modVal > 0.025 ? (0.05 - modVal) : -modVal;
                                attacksPerSecond = baseAttackSpeed * (attackSpeedIncr / 100 + 1);
                            }

                            PhysicalDPS = (PhysicalDPS / 2) * attacksPerSecond;
                            ElementalDPS = (ElementalDPS / 2) * attacksPerSecond;
                            ChaosDPS = (ChaosDPS / 2) * attacksPerSecond;

                            //20 퀄리티 보다 낮을땐 20 퀄리티 기준으로 계산
                            quality20Dps = quality20Dps < 20 ? PhysicalDPS * (PhysicalDamageIncr + 120) / (PhysicalDamageIncr + quality20Dps + 100) : 0;
                            PhysicalDPS = quality20Dps > 0 ? quality20Dps : PhysicalDPS;

                            lbDPS.Content = "DPS: P." + Math.Round(PhysicalDPS, 2).ToString() +
                                            " + E." + Math.Round(ElementalDPS, 2).ToString() +
                                            " = T." + Math.Round(PhysicalDPS + ElementalDPS + ChaosDPS, 2).ToString();
                        }

                        BaseResultData tmpBaseType = null;

                        if (is_captured_beast)
                        {
                            tmpBaseType = mMonsterDatas.Find(x => x.NameEn == itemType);

                            mItemBaseName.TypeEN = tmpBaseType == null ? itemType : tmpBaseType.NameEn;
                            mItemBaseName.NameEN = "";
                            itemName = "";
                        }
                        else
                        {
                            WordeResultData wordData = mWordDatas.Find(x => x.NameEn == itemName);
                            mItemBaseName.NameEN = wordData == null ? itemName : wordData.NameEn;

                            if (wordData == null && itemRarity == Restr.Rare)
                            {
                                string[] tmp = itemName.Split(' ');
                                if (tmp.Length > 1)
                                {
                                    int idx = 0;
                                    string tmp2 = "";

                                    for (int i = 0; i < tmp.Length; i++)
                                    {
                                        tmp2 += " " + tmp[i];
                                        tmp2 = tmp2.TrimStart();
                                        wordData = mWordDatas.Find(x => x.NameEn == tmp2);
                                        if (wordData != null)
                                        {
                                            idx = i + 1;
                                            mItemBaseName.NameEN = wordData.NameEn;
                                            break;
                                        }
                                    }

                                    tmp2 = "";
                                    for (int i = idx; i < tmp.Length; i++)
                                    {
                                        tmp2 += " " + tmp[i];
                                        wordData = mWordDatas.Find(x => x.NameEn == tmp2);
                                        if (wordData != null)
                                        {
                                            mItemBaseName.NameEN += wordData.NameEn;
                                            break;
                                        }
                                    }
                                }
                            }

                            tmpBaseType = mBaseDatas.Find(x => x.NameEn == itemType);
                            mItemBaseName.TypeEN = tmpBaseType == null ? itemType : tmpBaseType.NameEn;
                        }
                    }

                    mItemBaseName.NameEN = itemName;
                    mItemBaseName.TypeKR = is_captured_beast ? mItemBaseName.TypeEN : itemType;

                    if (Restr.ServerLang == 1)
                        cbName.Content = (mItemBaseName.NameEN + " " + mItemBaseName.TypeEN).Trim();
                    else
                        cbName.Content = (Regex.Replace(itemName, @"\([a-zA-Z\s']+\)$", "") + " " + Regex.Replace(itemType, @"\([a-zA-Z\s']+\)$", "")).Trim();

                    //cbName.IsChecked = (itemRarity != Restr.Rare && itemRarity != Restr.Magic) || !(by_type && mConfigData.Options.SearchByType);

                    if (itemRarity != Restr.Unique && itemRarity != Restr.Normal && !is_currency)
                        cbName.IsChecked = mConfigData.Options.SearchByType;
                    else
                        cbName.IsChecked = true;


                    if (is_elder_map)
                    {
                        cbName.IsChecked = false;
                    }
                    cbRarity.SelectedValue = itemRarity;
                    if (cbRarity.SelectedIndex == -1)
                    {
                        cbRarity.Items.Clear();
                        cbRarity.Items.Add(itemRarity);
                        cbRarity.SelectedIndex = cbRarity.Items.Count - 1;
                    }
                    else if ((string)cbRarity.SelectedValue == Restr.Normal)
                    {
                        cbRarity.SelectedIndex = 0;
                    }
                    
                    bool IsExchangeCurrency = inherit == "Currency" && Restr.lExchangeCurrency.ContainsKey(itemType);
                    bdExchange.Visibility = !is_gem && (is_detail || IsExchangeCurrency) ? Visibility.Visible : Visibility.Hidden;
                    bdExchange.IsEnabled = IsExchangeCurrency;

                    if (bdExchange.Visibility == Visibility.Hidden)
                    {
                        tbLvMin.Text = Regex.Replace(lItemOption[is_gem ? Restr.Lv : Restr.ItemLv].Trim(), "[^0-9]", "");
                        tbQualityMin.Text = item_quality;

                        string[] Influences = { Restr.Shaper, Restr.Elder, Restr.Crusader, Restr.Redeemer, Restr.Hunter, Restr.Warlord };
                        for (int i = 0; i < Influences.Length; i++)
                        {
                            if (lItemOption[Influences[i]] == "_TRUE_")
                                cbInfluence2.SelectedIndex = i + 1;
                        }

                        for (int i = 0; i < Influences.Length; i++)
                        {
                            if (cbInfluence2.SelectedIndex != (i + 1) && lItemOption[Influences[i]] == "_TRUE_")
                                cbInfluence1.SelectedIndex = i + 1;
                        }

                        if (lItemOption[Restr.Corrupt] == "_TRUE_")
                        {
                            cbCorrupt.BorderThickness = new Thickness(2);
                            //ckCorrupt.FontWeight = FontWeights.Bold;
                            //ckCorrupt.Foreground = System.Windows.Media.Brushes.DarkRed;
                        }

                        Synthesis.IsChecked = (is_map && is_blight) || lItemOption[Restr.Synthesis] == "_TRUE_";

                        if (is_map)
                        {
                            tbLvMin.Text = lItemOption[Restr.MaTier];
                            tbLvMax.Text = lItemOption[Restr.MaTier];
                            ckLv.Content = "Tier";
                            ckLv.IsChecked = is_elder_map ? false : true;
                            Synthesis.Content = "Blighted";
                        }
                        else if (is_gem)
                        {
                            ckLv.IsChecked = lItemOption[Restr.Lv].IndexOf(" (" + Restr.Max) > 0;
                            ckQuality.IsChecked = ckLv.IsChecked == true && item_quality != "" && int.Parse(item_quality) > 19;
                        }
                        else if (by_type && itemRarity == Restr.Normal)
                        {
                            ckLv.IsChecked = tbLvMin.Text != "" && int.Parse(tbLvMin.Text) > 82;
                        }
                    }

                    bdDetail.Visibility = is_detail ? Visibility.Visible : Visibility.Hidden;
                    lbSocketBackground.Visibility = by_type ? Visibility.Hidden : Visibility.Visible;

                    if (isWinShow || this.Visibility == Visibility.Visible)
                    {
                        PriceUpdateThreadWorker(GetItemOptions(), null, (string)cbAccountState.SelectedValue, 0);

                        tkPriceInfo1.Foreground = tkPriceInfo2.Foreground = System.Windows.SystemColors.WindowTextBrush;
                        tkPriceCount1.Foreground = tkPriceCount2.Foreground = System.Windows.SystemColors.WindowTextBrush;

                        this.ShowActivated = false;
                        this.Visibility = Visibility.Visible;
                    }
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine(ex.Message);
                ForegroundMessage(String.Format("{0} 에러:  {1}\r\n\r\n{2}\r\n\r\n", ex.Source, ex.Message, ex.StackTrace), "에러", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private ItemOption GetItemOptions()
        {
            ItemOption itemOption = new ItemOption();

            itemOption.Influence1 = (byte)cbInfluence1.SelectedIndex;
            itemOption.Influence2 = (byte)cbInfluence2.SelectedIndex;

            // 영향은 첫번째 값이 우선 순위여야 함
            if (itemOption.Influence1 == 0 && itemOption.Influence2 != 0)
            {
                itemOption.Influence1 = itemOption.Influence2;
                itemOption.Influence2 = 0;
            }

            itemOption.Corrupt = (byte)cbCorrupt.SelectedIndex;
            itemOption.Synthesis = Synthesis.IsChecked == true;
            itemOption.ChkSocket = ckSocket.IsChecked == true;
            itemOption.ChkQuality = ckQuality.IsChecked == true;
            itemOption.ChkLv = ckLv.IsChecked == true;
            itemOption.ByType = cbName.IsChecked != true;

            itemOption.SocketMin = StrToDouble(tbSocketMin.Text, 99999);
            itemOption.SocketMax = StrToDouble(tbSocketMax.Text, 99999);
            itemOption.LinkMin = StrToDouble(tbLinksMin.Text, 99999);
            itemOption.LinkMax = StrToDouble(tbLinksMax.Text, 99999);
            itemOption.QualityMin = StrToDouble(tbQualityMin.Text, 99999);
            itemOption.QualityMax = StrToDouble(tbQualityMax.Text, 99999);
            itemOption.LvMin = StrToDouble(tbLvMin.Text, 99999);
            itemOption.LvMax = StrToDouble(tbLvMax.Text, 99999);

            itemOption.PriceMin = tbPriceFilterMin.Text == "" ? 0 : StrToDouble(tbPriceFilterMin.Text, 99999);
            itemOption.Rarity = (string)cbRarity.SelectedValue;

            itemOption.ElderGuardian = cbElderGuardian.SelectedIndex;
            itemOption.MapInfluence = cbMapInfluence.SelectedIndex;

            int total_res_idx = -1;

            for (int i = 0; i < 10; i++)
            {
                Itemfilter itemfilter = new Itemfilter();
                ComboBox comboBox = (ComboBox)this.FindName("cbOpt" + i);
                
                if (comboBox.SelectedIndex > -1)
                {
                    double minValue = StrToDouble(((TextBox)this.FindName("tbOpt" + i + "_0")).Text, 99999);
                    double maxValue = StrToDouble(((TextBox)this.FindName("tbOpt" + i + "_1")).Text, 99999);
                    if ((comboBox.Text.ToLower() == "monster"))
                    {
                        ((TextBox)this.FindName("tbOpt" + i + "_0")).Text = metamorphMods[((TextBox)this.FindName("tbOpt" + i)).Text.Trim()];
                    }
                    itemfilter.text = ((TextBox)this.FindName("tbOpt" + i)).Text.Trim();
                    itemfilter.disabled = ((CheckBox)this.FindName("tbOpt" + i + "_2")).IsChecked != true;
                    itemfilter.min = minValue;
                    itemfilter.max = maxValue;
                    
                    if (itemfilter.text.Contains(Restr.Allocates))
                    {
                        itemfilter.option = (int)minValue;
                        itemfilter.min = 99999;
                    }

                    if (itemOption.ElderGuardian > 0 && itemfilter.text.Contains("occupied by"))
                        itemfilter.option = itemOption.ElderGuardian;
                    if (itemOption.MapInfluence > 0 && itemfilter.text.Contains("influenced by"))
                        itemfilter.option = itemOption.MapInfluence;

                    /*if (mItemBaseName.Inherits.Contains("Weapons"))
                    {
                        switch (itemfilter.text)
                        {
                            case Restr.AttackSpeedIncr:
                            case Restr.FlatPhysicalDamage:
                            case Restr.FlatAccuracyRating:
                            case Restr.FlatColdDamage:
                            case Restr.FlatLightningDamage:
                            case Restr.FlatFireDamage:
                            case Restr.FlatChaosDamage:
                                itemfilter.text = itemfilter.text + " (Local)";
                                break;
                        }
                    }*/

                    if (itemfilter.text == Restr.TotalResistance)
                    {
                        if (total_res_idx == -1)
                            total_res_idx = itemOption.itemfilters.Count;
                        else
                        {
                            itemOption.itemfilters[total_res_idx].min += itemfilter.min == 99999 ? 0 : itemfilter.min;
                            itemOption.itemfilters[total_res_idx].max += itemfilter.max == 99999 ? 0 : itemfilter.max;
                            continue;
                        }

                        itemfilter.id = "pseudo.pseudo_total_resistance";
                    }
                    else
                    {
                        itemfilter.id = ((FilterEntrie)comboBox.SelectedItem).ID;
                    }

                    itemOption.itemfilters.Add(itemfilter);
                }
            }

            return itemOption;
        }

        private string CreateJson(ItemOption itemOptions, bool useSaleType, string accountState)
        {
            string BeforeDayToString(int day)
            {
                if (day < 3)
                    return "1day";
                else if (day < 7)
                    return "3days";
                else if (day < 14)
                    return "1week";
                return "2weeks";
            }

            if (itemOptions.Rarity != null && itemOptions.Rarity != "")
            {
                try
                {
                    JsonData jsonData = new JsonData();
                    jsonData.Query = new q_Query();
                    q_Query JQ = jsonData.Query;
                    
                    JQ.Name = Restr.ServerLang == 1 ? mItemBaseName.NameEN : mItemBaseName.NameKR;
                    JQ.Type = Restr.ServerLang == 1 ? mItemBaseName.TypeEN : mItemBaseName.TypeKR;

                    string Inherit = mItemBaseName.Inherits.Length > 0 ? mItemBaseName.Inherits[0] : "";

                    JQ.Stats = new q_Stats[0];



                    JQ.Status.Option = accountState;
                    jsonData.Sort.Price = "asc";

                    JQ.Filters.Type.Filters.Rarity.Option = "any";
                    JQ.Filters.Type.Filters.Category.Option = "any";

                    JQ.Filters.Trade.Disabled = mConfigData.Options.SearchBeforeDay == 0;
                    JQ.Filters.Trade.Filters.Indexed.Option = mConfigData.Options.SearchBeforeDay == 0 ? "any" : BeforeDayToString(mConfigData.Options.SearchBeforeDay);
                    JQ.Filters.Trade.Filters.SaleType.Option = useSaleType ? "priced" : "any";
                    JQ.Filters.Trade.Filters.Price.Min = 99999;
                    JQ.Filters.Trade.Filters.Price.Max = 99999;

                    if (itemOptions.PriceMin > 0)
                    {
                        JQ.Filters.Trade.Filters.Price.Min = itemOptions.PriceMin;
                    }

                    JQ.Filters.Socket.Disabled = itemOptions.ChkSocket != true;

                    JQ.Filters.Socket.Filters.Links.Min = itemOptions.LinkMin;
                    JQ.Filters.Socket.Filters.Links.Max = itemOptions.LinkMax;
                    JQ.Filters.Socket.Filters.Sockets.Min = itemOptions.SocketMin;
                    JQ.Filters.Socket.Filters.Sockets.Max = itemOptions.SocketMax;

                    JQ.Filters.Misc.Filters.Quality.Min = itemOptions.ChkQuality == true ? itemOptions.QualityMin : 99999;
                    JQ.Filters.Misc.Filters.Quality.Max = itemOptions.ChkQuality == true ? itemOptions.QualityMax : 99999;

                    JQ.Filters.Misc.Filters.Ilvl.Min = itemOptions.ChkLv != true || Inherit == "Gems" || Inherit == "Maps" ? 99999 : itemOptions.LvMin;
                    JQ.Filters.Misc.Filters.Ilvl.Max = itemOptions.ChkLv != true || Inherit == "Gems" || Inherit == "Maps" ? 99999 : itemOptions.LvMax;
                    JQ.Filters.Misc.Filters.Gem_level.Min = itemOptions.ChkLv == true && Inherit == "Gems" ? itemOptions.LvMin : 99999;
                    JQ.Filters.Misc.Filters.Gem_level.Max = itemOptions.ChkLv == true && Inherit == "Gems" ? itemOptions.LvMax : 99999;

                    JQ.Filters.Misc.Filters.Shaper.Option = Inherit != "Maps" && (itemOptions.Influence1 == 1 || itemOptions.Influence2 == 1) ? "true" : "any";
                    JQ.Filters.Misc.Filters.Elder.Option = Inherit != "Maps" && (itemOptions.Influence1 == 2 || itemOptions.Influence2 == 2) ? "true" : "any";
                    JQ.Filters.Misc.Filters.Crusader.Option = Inherit != "Maps" && (itemOptions.Influence1 == 3 || itemOptions.Influence2 == 3) ? "true" : "any";
                    JQ.Filters.Misc.Filters.Redeemer.Option = Inherit != "Maps" && (itemOptions.Influence1 == 4 || itemOptions.Influence2 == 4) ? "true" : "any";
                    JQ.Filters.Misc.Filters.Hunter.Option = Inherit != "Maps" && (itemOptions.Influence1 == 5 || itemOptions.Influence2 == 5) ? "true" : "any";
                    JQ.Filters.Misc.Filters.Warlord.Option = Inherit != "Maps" && (itemOptions.Influence1 == 6 || itemOptions.Influence2 == 6) ? "true" : "any";

                    JQ.Filters.Misc.Filters.Synthesis.Option = Inherit != "Maps" && itemOptions.Synthesis == true ? "true" : "any";
                    JQ.Filters.Misc.Filters.Corrupted.Option = itemOptions.Corrupt == 1 ? "true" : (itemOptions.Corrupt == 2 ? "false" : "any");

                    JQ.Filters.Misc.Disabled = !(
                        itemOptions.ChkQuality == true || (Inherit != "Maps" && itemOptions.Influence1 != 0) || itemOptions.Corrupt != 0
                        || (Inherit != "Maps" && itemOptions.ChkLv == true) || (Inherit != "Maps" && itemOptions.Synthesis == true)
                    );

                    JQ.Filters.Map.Disabled = !(
                        Inherit == "Maps" && (itemOptions.ChkLv == true || itemOptions.Synthesis == true || itemOptions.Influence1 != 0)
                    );

                    JQ.Filters.Map.Filters.Tier.Min = itemOptions.ChkLv == true && Inherit == "Maps" ? itemOptions.LvMin : 99999;
                    JQ.Filters.Map.Filters.Tier.Max = itemOptions.ChkLv == true && Inherit == "Maps" ? itemOptions.LvMax : 99999;
                    JQ.Filters.Map.Filters.Shaper.Option = Inherit == "Maps" && itemOptions.Influence1 == 1 ? "true" : "any";
                    JQ.Filters.Map.Filters.Elder.Option = Inherit == "Maps" && itemOptions.Influence1 == 2 ? "true" : "any";
                    JQ.Filters.Map.Filters.Blight.Option = Inherit == "Maps" && itemOptions.Synthesis == true ? "true" : "any";

                    bool error_filter = false;

                    if (itemOptions.itemfilters.Count > 0)
                    {
                        JQ.Stats = new q_Stats[1];
                        JQ.Stats[0] = new q_Stats();
                        JQ.Stats[0].Type = "and";
                        JQ.Stats[0].Filters = new q_Stats_filters[itemOptions.itemfilters.Count];

                        int idx = 0;

                        for (int i = 0; i < itemOptions.itemfilters.Count; i++)
                        {
                            string input = itemOptions.itemfilters[i].text;
                            string id = itemOptions.itemfilters[i].id;
                            string type = itemOptions.itemfilters[i].id.Split('.')[0];
                            if (input.Trim() != "")
                            {
                                string type_name = Restr.lFilterTypeName[type];

                                FilterResultEntrie filter = null;
                                FilterResult filterResult = Array.Find(mFilterData.Result, x => x.Label == type_name);

                                input = Regex.Escape(input).Replace("\\+\\#", "[+]?\\#");

                                // 무기에 경우 pseudo_adds_[a-z]+_damage 옵션은 공격 시 가 붙음
                                if (type_name == Restr.Pseudo && Inherit == "Weapons" && Regex.IsMatch(id, @"^pseudo.pseudo_adds_[a-z]+_damage$"))
                                {
                                    id = id + "_to_attacks";
                                }
                                else if (type_name != Restr.Pseudo && (Inherit == "Weapons" || Inherit == "Armours"))
                                {
                                    // 장비 전용 옵션 (특정) 인 것인가 검사
                                    Regex rgx = new Regex("^" + input + "$", RegexOptions.IgnoreCase);
                                    FilterResultEntrie[] tmp_filters = Array.FindAll(filterResult.Entries, x => rgx.IsMatch(x.Text) && x.Type == type);
                                    if (tmp_filters.Length > 0) filter = tmp_filters[0];
                                }

                                if (filter == null)
                                {
                                    filter = Array.Find(filterResult.Entries, x => x.ID == id && x.Type == type && x.Part == null);
                                }

                                JQ.Stats[0].Filters[idx] = new q_Stats_filters();
                                JQ.Stats[0].Filters[idx].Value = new q_Min_And_Max();
                                JQ.Stats[0].Filters[idx].Value.option = 99999;
                                
                                if (filter != null && filter.ID != null && filter.ID.Trim() != "")
                                {
                                    JQ.Stats[0].Filters[idx].Disabled = itemOptions.itemfilters[i].disabled == true;
                                    
                                    if (itemOptions.itemfilters[i].option != 0)
                                    {
                                        JQ.Stats[0].Filters[idx].Value.option = itemOptions.itemfilters[i].option;
                                        JQ.Stats[0].Filters[idx].Value.Min = 99999;
                                        JQ.Stats[0].Filters[idx].Value.Max = 99999;
                                    }
                                    else
                                    {
                                        JQ.Stats[0].Filters[idx].Value.Min = itemOptions.itemfilters[i].min;
                                        JQ.Stats[0].Filters[idx].Value.Max = itemOptions.itemfilters[i].max;
                                    }

                                    if (!Inherit.Contains("Armours") && filter.ID == "explicit.stat_124859000") // #% increased Evasion Rating
                                    {
                                        JQ.Stats[0].Filters[idx++].Id = "explicit.stat_2106365538";
                                    }
                                    else if (Inherit.Contains("Armours") && filter.ID == "explicit.stat_2144192055") // # to Evasion Rating
                                    {
                                        JQ.Stats[0].Filters[idx++].Id = "explicit.stat_53045048";
                                    }
                                    else if (Inherit.Contains("Armours") && filter.ID == "explicit.stat_809229260") // # to Armour
                                    {
                                        JQ.Stats[0].Filters[idx++].Id = "explicit.stat_3484657501";
                                    }
                                    else if (!Inherit.Contains("Armours") && filter.ID == "explicit.stat_1062208444") // %# increased Armour
                                    {
                                        JQ.Stats[0].Filters[idx++].Id = "explicit.stat_2866361420";
                                    }
                                    else if (Inherit.Contains("Weapons") && filter.ID == "explicit.stat_803737631") // # to Accuracy Rating
                                    {
                                        JQ.Stats[0].Filters[idx++].Id = "explicit.stat_691932474";
                                    }
                                    else if (Inherit.Contains("Weapons") && filter.ID == "explicit.stat_795138349") // #% chance to poison on hit
                                    {
                                        JQ.Stats[0].Filters[idx++].Id = "explicit.stat_3885634897";
                                    }
                                    else
                                    {
                                        JQ.Stats[0].Filters[idx++].Id = filter.ID;
                                    }
                                }
                                else
                                {
                                    error_filter = true;
                                    itemOptions.itemfilters[i].isNull = true;

                                    // 오류 방지를 위해 널값시 아무거나 추가 
                                    JQ.Stats[0].Filters[idx].Disabled = true;
                                    JQ.Stats[0].Filters[idx].Value.Min = 99999;
                                    JQ.Stats[0].Filters[idx].Value.Max = 99999;
                                    JQ.Stats[0].Filters[idx++].Id = "temp_ids";
                                }
                            }
                        }
                    }

                    /*
                    if (!ckSocket.Dispatcher.CheckAccess())
                    else if (ckSocket.Dispatcher.CheckAccess())
                    */

                    if (Restr.lInherit.ContainsKey(Inherit))
                    {
                        string option = Restr.lInherit[Inherit];

                        if (itemOptions.ByType && Inherit == "Weapons" || Inherit == "Armours")
                        {
                            string[] tmp = mItemBaseName.Inherits;

                            if (tmp.Length > 2)
                            {
                                string tmp2 = tmp[Inherit == "Armours" ? 1 : 2].ToLower();

                                if (Inherit == "Weapons")
                                {
                                    tmp2 = tmp2.Replace("hand", "");
                                    tmp2 = tmp2.Remove(tmp2.Length - 1);
                                    if (tmp2 == "stave" && tmp.Length == 4)
                                    {
                                        if (tmp[3] == "AbstractWarstaff")
                                            tmp2 = "warstaff";
                                        else if (tmp[3] == "AbstractStaff")
                                            tmp2 = "staff";
                                    }
                                }
                                else if (Inherit == "Armours" && (tmp2 == "shields" || tmp2 == "helmets" || tmp2 == "bodyarmours"))
                                {
                                    if (tmp2 == "bodyarmours")
                                        tmp2 = "chest";
                                    else
                                        tmp2 = tmp2.Remove(tmp2.Length - 1);
                                }

                                option += "." + tmp2;
                            }
                        }

                        JQ.Filters.Type.Filters.Category.Option = option;
                    }

                    JQ.Filters.Type.Filters.Rarity.Option = "any";
                    if (Restr.lRarity.ContainsKey(itemOptions.Rarity))
                    {
                        JQ.Filters.Type.Filters.Rarity.Option = Restr.lRarity[itemOptions.Rarity];
                    }

                    string sEntity = Json.Serialize<JsonData>(jsonData);
                    if (itemOptions.ByType || JQ.Name == "" || itemOptions.Rarity != Restr.Unique)
                    {
                        sEntity = sEntity.Replace("\"name\":\"" + JQ.Name + "\",", "");

                        if (Inherit == "Jewels" || itemOptions.ByType)
                            sEntity = sEntity.Replace("\"type\":\"" + JQ.Type + "\",", "");
                        else if (Inherit == "Prophecies")
                            sEntity = sEntity.Replace("\"type\":\"" + JQ.Type + "\",", "\"name\":\"" + JQ.Type + "\",");
                    }

                    

                    sEntity = sEntity.Replace("{\"max\":99999,\"min\":99999}", "{}");
                    sEntity = sEntity.Replace("{\"max\":99999,", "{");
                    sEntity = sEntity.Replace(",\"min\":99999}", "}");
                    sEntity = sEntity.Replace("\"min\":99999,", "");

                    sEntity = sEntity.Replace(",{\"disabled\":true,\"id\":\"temp_ids\",\"value\":{}}", "");
                    sEntity = sEntity.Replace("[{\"disabled\":true,\"id\":\"temp_ids\",\"value\":{}}", "[");
                    sEntity = sEntity.Replace("[,", "[");

                    sEntity = Regex.Replace(sEntity, "\"(sale_type|rarity|category|corrupted|synthesised_item|shaper_item|elder_item|crusader_item|redeemer_item|hunter_item|warlord_item|map_shaped|map_elder|map_blighted)\":{\"option\":\"any\"},?", "");
                    sEntity = sEntity.Replace("},}", "}}");
                    sEntity = sEntity.Replace(",\"option\":99999", "");
                    
                    sEntity = sEntity.Replace("99999", "null");
                    sEntity = sEntity.Replace("\"value\":{\"option\":null", "\"value\":{");
                    //sEntity = sEntity.Replace("\"option\":null", "");
                    //sEntity = sEntity.Replace("\"implicit.stat_1792283443\",\"value\":{\"max\":0,\"min\":0}", "\"implicit.stat_1792283443\",\"value\":{\"option\":\"2\"}");
                    //sEntity = sEntity.Replace("\"implicit.stat_3624393862\",\"value\":{\"max\":0,\"min\":0}", "\"implicit.stat_3624393862\",\"value\":{\"option\":\"4\"}");

                    if (error_filter)
                    {
                        Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                            (ThreadStart)delegate ()
                            {
                                for (int i = 0; i < itemOptions.itemfilters.Count; i++)
                                {
                                    if (itemOptions.itemfilters[i].isNull)
                                    {
                                        ((TextBox)this.FindName("tbOpt" + i)).Background = System.Windows.Media.Brushes.Red;
                                        ((TextBox)this.FindName("tbOpt" + i + "_0")).Text = "error";
                                        ((TextBox)this.FindName("tbOpt" + i + "_1")).Text = "error";
                                        ((CheckBox)this.FindName("tbOpt" + i + "_2")).IsChecked = false;
                                        ((CheckBox)this.FindName("tbOpt" + i + "_2")).IsEnabled = false;
                                        ((CheckBox)this.FindName("tbOpt" + i + "_3")).Visibility = Visibility.Hidden;
                                    }
                                }
                            }
                        );
                    }
                    return sEntity;
                }
                catch (Exception ex)
                {
                    //Console.WriteLine(ex.Message);
                    ForegroundMessage(String.Format("{0} 에러:  {1}\r\n\r\n{2}\r\n\r\n", ex.Source, ex.Message, ex.StackTrace), "에러", MessageBoxButton.OK, MessageBoxImage.Error);
                    return "";
                }
            }
            else
            {
                return "";
            }
            
        }

        private void PriceUpdate(string[] entity, int listCount, string accountState, int minimumStock)
        {
            string result = "Waiting for results...";
            string result2 = "";
            string urlString = "";
            string sEentity;
            bool currencyExchange;
            if (entity.Length > 1)
            {
                sEentity = String.Format(
                        "{{\"exchange\":{{\"status\":{{\"option\":\"{0}\"}},\"have\":[\"{1}\"],\"want\":[\"{2}\"], \"minimum\": {3}}}}}",
                        accountState,
                        entity[0],
                        entity[1],
                        minimumStock
                    );
                urlString = Restr.ExchangeApi[Restr.ServerLang];
                currencyExchange = true;
            }
            else
            {
                sEentity = entity[0];
                
                urlString = Restr.TradeApi[Restr.ServerLang];
                currencyExchange = false;
            }
            
            if (sEentity != null && sEentity != "")
            {
                try
                {
                    string sResult = SendHTTP(sEentity, urlString + Restr.ServerType, mConfigData.Options.ServerTimeout);
                    result = "Something went wrong";

                    if (sResult != null)
                    {
                        ResultData resultData = Json.Deserialize<ResultData>(sResult);
                        Dictionary<string, int> currencys = new Dictionary<string, int>();

                        int total = 0;
                        int resultCount = resultData.Result.Length;
                        if (resultData.Result.Length > 0)
                        {
                            string ents0 = "", ents1 = "";

                            if (entity.Length > 1)
                            {
                                listCount = listCount + 2;
                                ents0 = Regex.Replace(entity[0], @"(timeless-)?([a-z]{3})[a-z\-]+\-([a-z]+)", @"$3`$2");
                                ents1 = Regex.Replace(entity[1], @"(timeless-)?([a-z]{3})[a-z\-]+\-([a-z]+)", @"$3`$2");
                            }

                            for (int x = 0; x < listCount; x++)
                            {
                                string[] tmp = new string[5];
                                int cnt = x * 5;
                                int length = 0;

                                if (cnt >= resultData.Result.Length)
                                    break;

                                for (int i = 0; i < 5; i++)
                                {
                                    if (i + cnt >= resultData.Result.Length)
                                        break;

                                    tmp[i] = resultData.Result[i + cnt];
                                    length++;
                                }

                                string jsonResult = "";
                                string url = Restr.FetchApi[Restr.ServerLang] + string.Join(",", tmp) + "?query=" + resultData.ID + (currencyExchange ? "&exchange" : "");
                                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri(url));
                                request.Timeout = 10000;

                                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                                using (StreamReader streamReader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                                {
                                    jsonResult = streamReader.ReadToEnd();
                                }

                                if (jsonResult != "")
                                {
                                    FetchData fetchData = new FetchData();
                                    fetchData.Result = new FetchDataInfo[5];

                                    fetchData = Json.Deserialize<FetchData>(jsonResult);
                                    
                                    for (int i = 0; i < fetchData.Result.Length; i++)
                                    {
                                        
                                        if (fetchData.Result[i] == null)
                                            break;

                                        if ((!currencyExchange && (fetchData.Result[i].Listing.Price != null && fetchData.Result[i].Listing.Price.Amount > 0)) ||
                                            (currencyExchange && (fetchData.Result[i].Listing.Price != null && fetchData.Result[i].Listing.Price.Exchange != null &&
                                            fetchData.Result[i].Listing.Price.Item != null)))
                                        {
                                            string account = fetchData.Result[i].Listing.Account.Name;

                                            string onlineStatus = "Online";
                                            if (accountState == "any") {
                                                if (fetchData.Result[i].Listing.Account.Online == null)
                                                    onlineStatus = "Offline"; 
                                            }

                                            string buyerCurrency = "";
                                            double buyerAmount = 0;
                                            string sellerCurrency = "";
                                            double sellerAmount = 0;
                                            int sellerStock = 0;

                                            string key = "";
                                            double amount = 0;
                                            string keyName = "";
                                            if (currencyExchange)
                                            {

                                                buyerCurrency = fetchData.Result[i].Listing.Price.Exchange.Currency;
                                                buyerAmount = fetchData.Result[i].Listing.Price.Exchange.Amount;

                                                sellerCurrency = fetchData.Result[i].Listing.Price.Item.Currency;
                                                sellerAmount = fetchData.Result[i].Listing.Price.Item.Amount;
                                                sellerStock = fetchData.Result[i].Listing.Price.Item.Stock;

                                                key = fetchData.Result[i].Listing.Price.Exchange.Currency;
                                                amount = fetchData.Result[i].Listing.Price.Exchange.Amount;
                                                keyName = Restr.lExchangeCurrency.ContainsValue(key) ? Restr.lExchangeCurrency.FirstOrDefault(o => o.Value == key).Value : key;
                                            }
                                            else
                                            {

                                                key = fetchData.Result[i].Listing.Price.Currency;
                                                amount = fetchData.Result[i].Listing.Price.Amount;
                                                keyName = Restr.lExchangeCurrency.ContainsValue(key) ? Restr.lExchangeCurrency.FirstOrDefault(o => o.Value == key).Value : key;
                                            }
                                            string text = "";
                                            liPrice.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                                                (ThreadStart)delegate ()
                                                {
                                                    if (entity.Length > 1)
                                                    {

                                                        
                                                        string tName2 = Restr.lExchangeCurrency.ContainsValue(entity[1])
                                                                        ? Restr.lExchangeCurrency.FirstOrDefault(o => o.Value == entity[1]).Value : entity[1];

                                                        string ratioString = "";
                                                        double ratio = 0;
                                                        //text = Math.Round(1 / amount, 4) + " " + tName2 + " <-> " + Math.Round(amount, 4) + " " + keyName + " [" + account + "]" + " [" + onlineStatus + "]";
                                                        if (sellerAmount != 1 && buyerAmount != 1)
                                                        {
                                                            if (sellerAmount > buyerAmount)
                                                            {
                                                                ratio = (sellerAmount / buyerAmount);
                                                                ratioString = " [" + String.Format("{0:0.00}", ratio) + " " + sellerCurrency + " per] ";
                                                            } else
                                                            {
                                                                ratio = (buyerAmount / sellerAmount);
                                                                ratioString = " [" + String.Format("{0:0.00}", ratio) + " " + buyerCurrency + " per] ";
                                                            }
                                                        }
                                                        text = "[Stock:" + sellerStock + "], " + sellerAmount + " " + sellerCurrency + " <= " + buyerAmount + " " + buyerCurrency + ratioString + " [" + account + "]";
                                                        if (cbSameUser.IsChecked == true)
                                                        {
                                                            if (liPrice.Items.Count > 1)
                                                            {
                                                                ListBoxItem lbi = (ListBoxItem)liPrice.Items[liPrice.Items.Count - 1];
                                                                if (!lbi.Content.ToString().Contains("[" + account + "]"))
                                                                    liPrice.Items.Add(new ListBoxItem { Content = text, Foreground = onlineStatus == "Online" ? System.Windows.Media.Brushes.Green : System.Windows.Media.Brushes.Red, BorderThickness = new Thickness(1) });
                                                            }
                                                            else
                                                            {
                                                                liPrice.Items.Add(new ListBoxItem { Content = text, Foreground = onlineStatus == "Online" ? System.Windows.Media.Brushes.Green : System.Windows.Media.Brushes.Red, BorderThickness = new Thickness(1) });
                                                            }
                                                        }
                                                        else
                                                        {
                                                            liPrice.Items.Add(new ListBoxItem { Content = text, Foreground = onlineStatus == "Online" ? System.Windows.Media.Brushes.Green : System.Windows.Media.Brushes.Red, BorderThickness = new Thickness(1) });
                                                        }
                                                    }
                                                    else
                                                    {
                                                        
                                                        text = amount + " " + keyName + " [" + account + "]" + " [" + onlineStatus + "]";
                                                        if (cbSameUser.IsChecked == true)
                                                        {
                                                            if (liPrice.Items.Count > 1)
                                                            {
                                                                ListBoxItem lbi = (ListBoxItem)liPrice.Items[liPrice.Items.Count - 1];
                                                                if (!lbi.Content.ToString().Contains("[" + account + "]"))
                                                                    liPrice.Items.Add(new ListBoxItem { Content = text, Foreground = onlineStatus == "Online" ? System.Windows.Media.Brushes.Green : System.Windows.Media.Brushes.Red, BorderThickness = new Thickness(1) });
                                                            }
                                                            else
                                                            {
                                                                liPrice.Items.Add(new ListBoxItem { Content = text, Foreground = onlineStatus == "Online" ? System.Windows.Media.Brushes.Green : System.Windows.Media.Brushes.Red, BorderThickness = new Thickness(1) });
                                                            }
                                                        } else
                                                        {
                                                            liPrice.Items.Add(new ListBoxItem { Content = text, Foreground = onlineStatus == "Online" ? System.Windows.Media.Brushes.Green : System.Windows.Media.Brushes.Red, BorderThickness = new Thickness(1) });
                                                        }
                                                    }
                                                }
                                            );

                                            if (entity.Length > 1)
                                                key = amount < 1 ? Math.Round(1 / amount, 1) + " " + ents1 : Math.Round(amount, 1) + " " + ents0;
                                            else
                                                key = Math.Round(amount - 0.1) + " " + key;
                                            if (currencys.ContainsKey(key))
                                                currencys[key]++;
                                            else
                                                currencys.Add(key, 1);

                                            total++;
                                        }
                                    }
                                }
                            }

                            if (currencys.Count > 0)
                            {
                                List<KeyValuePair<string, int>> myList = new List<KeyValuePair<string, int>>(currencys);
                                string first = ((KeyValuePair<string, int>)myList[0]).Key;
                                string last = ((KeyValuePair<string, int>)myList[myList.Count - 1]).Key;

                                myList.Sort(
                                    delegate (KeyValuePair<string, int> firstPair,
                                    KeyValuePair<string, int> nextPair)
                                    {
                                        return -1 * firstPair.Value.CompareTo(nextPair.Value);
                                    }
                                );

                                KeyValuePair<string, int> firstKey = myList[myList.Count - 1];
                                if (myList.Count > 1 && (firstKey.Value == 1 || (firstKey.Value == 2 && first == firstKey.Key)))
                                {
                                    int idx = myList.Count - 2;

                                    if (firstKey.Value == 1 || myList[idx].Value == 1)
                                        idx = (int)Math.Truncate((double)myList.Count / 2);

                                    firstKey = myList[idx];
                                }

                                result = Regex.Replace(first + " ~ " + last, @"(timeless-)?([a-z]{3})[a-z\-]+\-([a-z]+)", @"$3`$2");

                                for (int i = 0; i < myList.Count; i++)
                                {
                                    if (i == 2) break;
                                    if (myList[i].Value < 2) continue;
                                    result2 += myList[i].Key + "[" + myList[i].Value + "], ";
                                }

                                result2 = Regex.Replace(result2.TrimEnd(',', ' '), @"(timeless-)?([a-z]{3})[a-z\-]+\-([a-z]+)", @"$3`$2");
                                if (result2 == "")
                                    result2 = "Low result count, price will vary";
                            }
                        }

                        cbPriceListTotal.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                            (ThreadStart)delegate ()
                            {
                                cbPriceListTotal.Text = total + "/" + resultCount + " Search";
                            }
                        );

                        tkPriceCount1.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                            (ThreadStart)delegate ()
                            {
                                tkPriceCount1.Text = total > 0 ? total + (resultCount > total ? "+" : ".") : "";
                            }
                        );

                        tkPriceCount2.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                            (ThreadStart)delegate ()
                            {
                                tkPriceCount2.Text = total > 0 ? total + (resultCount > total ? "+" : ".") : "";
                            }
                        );

                        if ((resultData.Total == 0 || currencys.Count == 0))
                        {
                            result = "There is no results.";
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            tkPriceInfo1.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                (ThreadStart)delegate ()
                {
                    if (!currencyExchange)
                        tkPriceInfo1.Text = result + (result2 != "" ? " = " + result2 : "");
                    else
                        tkPriceInfo1.Text = "Check detailed tab";
                }
            );

            tkPriceInfo2.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                (ThreadStart)delegate ()
                {
                    if (!currencyExchange)
                        tkPriceInfo2.Text = result + (result2 != "" ? " = " + result2 : "");
                    else
                        tkPriceInfo2.Text = "Results loaded";
                }
            );

            liPrice.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                (ThreadStart)delegate ()
                {
                    if (liPrice.Items.Count == 0)
                        liPrice.Items.Add(result + (result2 != "" ? " = " + result2 : ""));
                }
            );
        }

        private Thread priceThread = null;

        private void PriceUpdateThreadWorker(ItemOption itemOptions, string[] exchange, string accountState, int minimumStock)
        {
            tkPriceInfo1.Text = tkPriceInfo2.Text = "Price checking...";
            tkPriceCount1.Text = tkPriceCount2.Text = "";
            cbPriceListTotal.Text = "0/0 Search";
            liPrice.Items.Clear();

            int listCount = (cbPriceListCount.SelectedIndex + 1) * 4;
            priceThread?.Interrupt();
            priceThread?.Abort();
            priceThread = new Thread(() => PriceUpdate(
                    exchange != null ? exchange : new string[1] { CreateJson(itemOptions, true, accountState) },
                    listCount,
                    accountState,
                    minimumStock
                ));
            priceThread.Start();
        }

        private string GetClipText(bool isUnicode)
        {
            return Clipboard.GetText(isUnicode ? TextDataFormat.UnicodeText : TextDataFormat.Text);
        }

        private void SetClipText(string text, TextDataFormat textDataFormat)
        {
            var ClipboardThread = new Thread(() =>
            {
                for (int i = 0; i < 10; i++)
                {
                    try
                    {
                        Clipboard.SetText(text, textDataFormat);
                        return;
                    }
                    catch { }
                    Thread.Sleep(10);
                }
            });
            ClipboardThread.SetApartmentState(ApartmentState.STA);
            ClipboardThread.IsBackground = false;
            ClipboardThread.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (Native.GetForegroundWindow().Equals(Native.FindWindow(Restr.PoeClass, Restr.PoeCaption)))
            {
                if (!mIsHotKey) InstallRegisterHotKey();

                if (!mIsPause && mConfigData.Options.CtrlWheel)
                {
                    TimeSpan dateDiff = Convert.ToDateTime(DateTime.Now) - MouseHookCallbackTime;
                    if (dateDiff.Ticks > 3000000000) // 5분간 마우스 움직임이 없으면 훜이 풀렸을 수 있어 다시...
                    {
                        MouseHookCallbackTime = Convert.ToDateTime(DateTime.Now);
                        MouseHook.Start();
                    }
                }
            }
            else
            {
                if (mIsHotKey) RemoveRegisterHotKey();
            }
        }

        private void MouseEvent(object sender, EventArgs e)
        {
            if (!mHotkeyProcBlock)
            {
                mHotkeyProcBlock = true;

                try
                {
                    int zDelta = ((MouseHook.MouseEventArgs)e).zDelta;
                    if (zDelta != 0)
                        System.Windows.Forms.SendKeys.SendWait(zDelta > 0 ? "{Left}" : "{Right}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

                mHotkeyProcBlock = false;
            }
        }

        private void InstallRegisterHotKey()
        {
            mIsHotKey = true;

            // 0x0: None, 0x1: Alt, 0x2: Ctrl, 0x3: Shift
            for (int i = 0; i < mConfigData.Shortcuts.Length; i++)
            {
                ConfigShortcut shortcut = mConfigData.Shortcuts[i];
                if (shortcut.Keycode > 0 && (shortcut.Value ?? "") != "")
                    Native.RegisterHotKey(mMainHwnd, 10001 + i, (uint)(shortcut.Ctrl ? 0x2 : 0x0), (uint)Math.Abs(shortcut.Keycode));
            }
        }

        private void RemoveRegisterHotKey()
        {
            mIsHotKey = false;

            for (int i = 0; i < mConfigData.Shortcuts.Length; i++)
            {
                ConfigShortcut shortcut = mConfigData.Shortcuts[i];
                if (shortcut.Keycode > 0 && (shortcut.Value ?? "") != "")
                    Native.UnregisterHotKey(mMainHwnd, 10001 + i);
            }
        }

        private bool mHotkeyProcBlock = false;
        private bool mClipboardBlock = false;

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == Native.WM_DRAWCLIPBOARD)
            {
                IntPtr findHwnd = Native.FindWindow(Restr.PoeClass, Restr.PoeCaption);

                if (!mIsPause && !mClipboardBlock && Native.GetForegroundWindow().Equals(findHwnd))
                {
                    try
                    {
                        if (Clipboard.ContainsText(TextDataFormat.UnicodeText) || Clipboard.ContainsText(TextDataFormat.Text))
                            ItemTextParser(GetClipText(Clipboard.ContainsText(TextDataFormat.UnicodeText)));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
            else if (!mHotkeyProcBlock && msg == (int)0x312) //WM_HOTKEY
            {
                mHotkeyProcBlock = true;

                IntPtr findHwnd = Native.FindWindow(Restr.PoeClass, Restr.PoeCaption);

                if (Native.GetForegroundWindow().Equals(findHwnd))
                {
                    int keyIdx = wParam.ToInt32();

                    string popWinTitle = "이곳을 잡고 이동, 이미지 클릭시 닫힘";
                    ConfigShortcut shortcut = mConfigData.Shortcuts[keyIdx - 10001];

                    if (shortcut != null && shortcut.Value != null)
                    {
                        string valueLower = shortcut.Value.ToLower();

                        try
                        {
                            if (valueLower == "{pause}")
                            {
                                mIsPause = !mIsPause;

                                if (mIsPause)
                                {
                                    if (mConfigData.Options.CtrlWheel)
                                        MouseHook.Stop();

                                    MessageBox.Show(Application.Current.MainWindow, "Hotkeys have been paused." + '\n' +
                                        "다시 시작하려면 일시 중지 단축키를 한번더 누르세요.", "POE 거래소 검색");
                                }
                                else
                                {
                                    if (mConfigData.Options.CtrlWheel)
                                        MouseHook.Start();

                                    MessageBox.Show(Application.Current.MainWindow, "Hotkeys have been re-enabled.", "POE 거래소 검색");
                                }

                                Native.SetForegroundWindow(findHwnd);
                            }
                            else if (valueLower == "{close}")
                            {
                                IntPtr pHwnd = Native.FindWindow(null, popWinTitle);

                                if (this.Visibility == Visibility.Hidden && pHwnd.ToInt32() == 0)
                                {
                                    Native.SendMessage(findHwnd, 0x0101, new IntPtr(shortcut.Keycode), IntPtr.Zero);
                                }
                                else
                                {
                                    if (pHwnd.ToInt32() != 0)
                                        Native.SendMessage(pHwnd, /* WM_CLOSE = */ 0x10, IntPtr.Zero, IntPtr.Zero);

                                    if (this.Visibility == Visibility.Visible)
                                        Close();
                                }
                            }
                            else if (!mIsPause)
                            {
                                if (valueLower == "{run}" || valueLower == "{wiki}")
                                {
                                    mClipboardBlock = true;

                                    System.Windows.Forms.SendKeys.SendWait("^{c}");
                                    Thread.Sleep(300);

                                    try
                                    {
                                        if (Clipboard.ContainsText(TextDataFormat.UnicodeText) || Clipboard.ContainsText(TextDataFormat.Text))
                                        {
                                            ItemTextParser(GetClipText(Clipboard.ContainsText(TextDataFormat.UnicodeText)), valueLower != "{wiki}");

                                            if (valueLower == "{wiki}")
                                                Button_Click_4(null, new RoutedEventArgs());
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine(ex.Message);
                                    }

                                    mClipboardBlock = false;
                                }
                                else if (valueLower.IndexOf("{enter}") == 0)
                                {
                                    //Regex regex = new Regex(@"{enter}", RegexOptions.IgnoreCase);
                                    //string tmp = regex.Replace(shortcut.Value, "" + '\n');
                                    //string[] strs = tmp.Trim().Split('\n');
                                    System.Windows.Forms.SendKeys.SendWait(valueLower);
                                    /*for (int i = 0; i < strs.Length; i++)
                                    {
                                        SetClipText(strs[i], TextDataFormat.UnicodeText);
                                        Thread.Sleep(300);
                                        System.Windows.Forms.SendKeys.SendWait("{enter}");
                                        System.Windows.Forms.SendKeys.SendWait("^{a}");
                                        System.Windows.Forms.SendKeys.SendWait("^{v}");
                                        System.Windows.Forms.SendKeys.SendWait("{enter}");
                                    }*/
                                }
                                else if (valueLower.IndexOf("{invite}") == 0)
                                {
                                    System.Windows.Forms.SendKeys.SendWait("^{ENTER}");
                                    System.Windows.Forms.SendKeys.SendWait("{HOME}");
                                    System.Windows.Forms.SendKeys.SendWait("{DELETE}");
                                    System.Windows.Forms.SendKeys.SendWait("/invite ");
                                    System.Windows.Forms.SendKeys.SendWait("{ENTER}");
                                }
                                else if (valueLower.IndexOf("{tradewith}") == 0)
                                {
                                    System.Windows.Forms.SendKeys.SendWait("^{ENTER}");
                                    System.Windows.Forms.SendKeys.SendWait("{HOME}");
                                    System.Windows.Forms.SendKeys.SendWait("{DELETE}");
                                    System.Windows.Forms.SendKeys.SendWait("/tradewith ");
                                    System.Windows.Forms.SendKeys.SendWait("{ENTER}");
                                }
                                else if (valueLower.IndexOf("{link}") == 0)
                                {
                                    Regex regex = new Regex(@"{link}", RegexOptions.IgnoreCase);
                                    string tmp = regex.Replace(shortcut.Value, "" + '\n');
                                    string[] strs = tmp.Trim().Split('\n');
                                    if (strs.Length > 0) Process.Start(strs[0]);
                                }
                                else if (valueLower.IndexOf(".jpg") > 0)
                                {
                                    IntPtr pHwnd = Native.FindWindow(null, popWinTitle);
                                    if (pHwnd.ToInt32() != 0)
                                        Native.SendMessage(pHwnd, /* WM_CLOSE = */ 0x10, IntPtr.Zero, IntPtr.Zero);

                                    PopWindow popWindow = new PopWindow(shortcut.Value);

                                    if ((shortcut.Position ?? "") != "")
                                    {
                                        string[] strs = shortcut.Position.ToLower().Split('x');
                                        popWindow.WindowStartupLocation = WindowStartupLocation.Manual;
                                        popWindow.Left = double.Parse(strs[0]);
                                        popWindow.Top = double.Parse(strs[1]);
                                    }

                                    popWindow.Title = popWinTitle;
                                    popWindow.Show();
                                }
                            }
                        }
                        catch (Exception)
                        {
                            ForegroundMessage("잘못된 단축키 명령입니다.", "단축키 에러", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }

                    handled = true;
                }

                mHotkeyProcBlock = false;
            }

            return IntPtr.Zero;
        }
    }
}
