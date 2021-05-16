using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.ComponentModel;
using System.Collections.ObjectModel;

using Microsoft.Win32;

using GTStandardDefinitionEditor.Entities;

namespace GTStandardDefinitionEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public Dictionary<string, string> ParamDescription = new Dictionary<string, string>();
        public StandardDefinition Database { get; set; }
        public SDEFListing CurrentParameter { get; set; }
        public string LastFile { get; set; }
        public MainWindow()
        {
            LoadDefinitions();
            InitializeComponent();
        }

        public bool LoadDefinitions()
        {
            ParamDescription.Clear();
            if (File.Exists("params_db.txt"))
            {
                string[] lines = File.ReadAllLines("params_db.txt");
                foreach (var line in lines)
                {
                    if (string.IsNullOrEmpty(line) || line.StartsWith("//"))
                        continue;

                    string[] spl = line.Split('|');

                    if (spl.Length < 2)
                        continue;

                    if (!ParamDescription.ContainsKey(spl[0]))
                        ParamDescription.Add(spl[0], spl[1]);
                }
            }
            else
                return false;

            return true;
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            var openDialog = new OpenFileDialog();
            openDialog.Filter = "Gran Turismo SDEF File (*.*)|*.*";
            openDialog.CheckFileExists = true;
            openDialog.CheckPathExists = true;

            if (openDialog.ShowDialog() == true)
            {
                try
                {
                    Database = SDEFMetaData.FromFile(openDialog.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error occured while loading file: {ex.Message}", "A not so friendly prompt", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                BuildTree();
                menuItem_Save.IsEnabled = true;
                menuItem_SaveDirect.IsEnabled = false;
                LastFile = null;

                tb_Status.Text = $"{DateTime.Now} - Loaded SDEF file with {Database.ParameterList.Count} parameters";
            }
        }

        private void SaveFile_Click(object sender, RoutedEventArgs e)
        {
            var saveDialog = new SaveFileDialog();
            saveDialog.Filter = "Gran Turismo SDEF File (*.*)|*.*";

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    Database.Save(saveDialog.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error occured while saving file: {ex.Message}", "A not so friendly prompt", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                LastFile = saveDialog.FileName;
                menuItem_SaveDirect.IsEnabled = true;
                tb_Status.Text = $"{DateTime.Now} - Saved SDEF file as {saveDialog.FileName}";
            }
        }

        private void SaveDirect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Database.Save(LastFile);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error occured while saving file: {ex.Message}", "A not so friendly prompt", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            tb_Status.Text = $"{DateTime.Now} - Saved SDEF file as {LastFile}";
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (menuItem_SaveDirect.IsEnabled is true)
                    SaveDirect_Click(sender, e);
            }
        }
        private void menuItem_ReloadDefs_Clicked(object sender, RoutedEventArgs e)
        {
            if (!LoadDefinitions())
                tb_Status.Text = $"{DateTime.Now} - Could not find params_db.txt";
            else
                tb_Status.Text = $"{DateTime.Now} - Loaded {ParamDescription.Count} definitions";
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Credits:\n" +
                "- Nenkai#9075 - Tool creator & reverse engineering the format\n" +
                "- TheAdmiester - Initial reverse engineering on AES file, which greatly helped reversing the data flow", "About", MessageBoxButton.OK);
        }

        private void tv_SDEFListing_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var item = (SDEFListing)e.NewValue;
            CurrentParameter = item;

            ResetEntryControls();

            if (CurrentParameter?.Entry is null && CurrentParameter?.ArrayElement is null)
                return;

            if (CurrentParameter?.Entry?.NodeType == NodeType.RawValueArray)
                return; // Don't need, its the def, not the values

            if (CurrentParameter?.Entry?.NodeType == NodeType.CustomType || CurrentParameter?.Entry?.NodeType == NodeType.CustomTypeArray)
            {
                paramType.Content = CurrentParameter.Entry.CustomTypeName;
                if (CurrentParameter.Entry != null)
                {
                    parameterName.Content = CurrentParameter.Entry.Name;
                    if (ParamDescription.TryGetValue(CurrentParameter.Entry.Name, out string desc))
                        parameterDescription.Text = desc;
                    else
                        parameterDescription.Text = "No description available";
                }
            }
            else 
            {
                SDEFVariant value = CurrentParameter?.Entry?.NodeType == NodeType.RawValue ? (CurrentParameter.Entry as SDEFParam).RawValue : CurrentParameter.ArrayElement;
                paramType.Content = value.Type.ToString();
                switch (value.Type)
                {
                    case Entities.ValueType.Byte:
                        param_Byte.IsEnabled = true;
                        param_Byte.Value = value.GetByte(); break;
                    case Entities.ValueType.SByte:
                        param_SByte.IsEnabled = true;
                        param_SByte.Value = value.GetSByte(); break;
                    case Entities.ValueType.Bool:
                        param_Bool.IsEnabled = true;
                        param_Bool.IsChecked = value.GetBool(); break;
                    case Entities.ValueType.Int:
                        param_Integer.IsEnabled = true;
                        param_Integer.Value = value.GetInt(); break;
                    case Entities.ValueType.UInt:
                        param_UInteger.IsEnabled = true;
                        param_UInteger.Value = value.GetUInt(); break;
                    case Entities.ValueType.Float:
                        param_Single.IsEnabled = true;
                        param_Single.Value = value.GetFloat(); break;
                    case Entities.ValueType.Double:
                        param_Double.IsEnabled = true;
                        param_Double.Value = value.GetDouble(); break;
                    case Entities.ValueType.ULong:
                        param_ULong.IsEnabled = true;
                        param_ULong.Value = value.GetULong(); break;
                    case Entities.ValueType.String:
                        param_String.IsEnabled = true;
                        param_String.Text = value.GetString(); break;
                    default:
                        break;
                }

                if (CurrentParameter.Entry != null)
                {
                    parameterName.Content = CurrentParameter.Entry.Name;
                    if (ParamDescription.TryGetValue(CurrentParameter.Entry.Name, out string desc))
                        parameterDescription.Text = desc;
                    else
                        parameterDescription.Text = "No description available";
                }
            }
            
        }

        private void param_Double_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (param_Double.Value != null)
            {
                var val = CurrentParameter.ArrayElement ?? (CurrentParameter.Entry as SDEFParam).RawValue;
                val.Set(param_Double.Value.Value);
                CurrentParameter.Name = CurrentParameter.ToString();
            }
        }

        private void param_Single_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (param_Single.Value != null)
            {
                var val = CurrentParameter.ArrayElement ?? (CurrentParameter.Entry as SDEFParam).RawValue;
                val.Set(param_Single.Value.Value);
                CurrentParameter.Name = CurrentParameter.ToString();
            }
        }

        private void param_UInteger_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (param_UInteger.Value != null)
            {
                var val = CurrentParameter.ArrayElement ?? (CurrentParameter.Entry as SDEFParam).RawValue;
                val.Set(param_UInteger.Value.Value);
                CurrentParameter.Name = CurrentParameter.ToString();
            }
        }

        private void param_SByte_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (param_SByte.Value != null)
            {
                var val = CurrentParameter.ArrayElement ?? (CurrentParameter.Entry as SDEFParam).RawValue;
                val.Set(param_SByte.Value.Value);
                CurrentParameter.Name = CurrentParameter.ToString();
            }
        }

        private void param_Byte_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (param_Byte.Value != null)
            {
                var val = CurrentParameter.ArrayElement ?? (CurrentParameter.Entry as SDEFParam).RawValue;
                val.Set(param_Byte.Value.Value);
                CurrentParameter.Name = CurrentParameter.ToString();
            }
        }

        private void param_Integer_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (param_Integer.Value != null)
            {
                var val = CurrentParameter.ArrayElement ?? (CurrentParameter.Entry as SDEFParam).RawValue;
                val.Set(param_Integer.Value.Value);
                CurrentParameter.Name = CurrentParameter.ToString();
            }
        }

        private void param_ULong_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (param_ULong.Value != null)
            {
                var val = CurrentParameter.ArrayElement ?? (CurrentParameter.Entry as SDEFParam).RawValue;
                val.Set(param_ULong.Value.Value);
                CurrentParameter.Name = CurrentParameter.ToString();
            }
        }

        private void param_String_ValueChanged(object sender, RoutedEventArgs e)
        {
            if (param_String.Text != null)
            {
                var val = CurrentParameter.ArrayElement ?? (CurrentParameter.Entry as SDEFParam).RawValue;
                val.Set(param_String.Text);
                CurrentParameter.Name = CurrentParameter.ToString();
            }
        }


        private void param_Bool_Checked(object sender, RoutedEventArgs e)
        {
            if (param_Bool.IsChecked != null)
            {
                var val = CurrentParameter.ArrayElement ?? (CurrentParameter.Entry as SDEFParam).RawValue;
                val.Set(param_Bool.IsChecked.Value);
                CurrentParameter.Name = CurrentParameter.ToString();
            }
        }

        public void BuildTree()
        {
            var root = new SDEFListing();
            tv_SDEFListing.Items.Clear();
            root.Name = $"Root - {Database.ParameterRoot.CustomTypeName}";
            BuildChildTree(root, Database.ParameterRoot);
            tv_SDEFListing.Items.Add(root);
        }

        public void BuildChildTree(SDEFListing item, SDEFBase current)
        {
            foreach (var entry in current.ChildParameters)
            {
                var listing = new SDEFListing();
                listing.Entry = entry;
                listing.Name = listing.ToString();
                if (entry.NodeType == NodeType.CustomType)
                {
                    BuildChildTree(listing, entry);
                }
                else if (entry.NodeType == NodeType.CustomTypeArray)
                {
                    SDEFParamArray array = entry as SDEFParamArray;
                    for (int i = 0; i < array.Values.Count; i++)
                    {
                        SDEFBase element = array.Values[i];
                        var arrValueListing = new SDEFListing() { Name = $"[{i}] - {array.CustomTypeName}" };
                        BuildChildTree(arrValueListing, element);
                        listing.Items.Add(arrValueListing);
                    }
                }
                else if (entry.NodeType == NodeType.RawValueArray)
                {
                    SDEFParamArray array = entry as SDEFParamArray;
                    for (int i = 0; i < array.RawValuesArray.Length; i++)
                    {
                        SDEFVariant elem = array.RawValuesArray[i];
                        var arrValueListing = new SDEFListing() { Name = $"[{i}] - {elem.ToString()}" };
                        arrValueListing.ArrayElement = elem;
                        arrValueListing.ArrayElementIndex = i;

                        listing.Items.Add(arrValueListing);
                    }
                }

                item.Items.Add(listing);
            }
        }

        public void ResetEntryControls()
        {
            param_Byte.IsEnabled = false;
            param_Byte.Value = null;

            param_SByte.IsEnabled = false;
            param_SByte.Value = null;

            param_Bool.IsEnabled = false;
            param_Bool.IsChecked = null;

            param_Integer.IsEnabled = false;
            param_Integer.Value = null;

            param_UInteger.IsEnabled = false;
            param_UInteger.Value = null;

            param_Single.IsEnabled = false;
            param_Single.Value = null;

            param_Double.IsEnabled = false;
            param_Double.Value = null;

            param_ULong.IsEnabled = false;
            param_ULong.Value = null;

            param_String.IsEnabled = false;
            param_String.Text = null;
        }
    }

    public class SDEFListing : INotifyPropertyChanged
    {
        public SDEFListing()
        {
            this.Items = new ObservableCollection<SDEFListing>();
        }

        private string _name;
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                Notify("Name");
            }
        }

        public SDEFVariant ArrayElement { get; set; }
        public int ArrayElementIndex { get; set; }

        public SDEFBase Entry { get; set; }

        public ObservableCollection<SDEFListing> Items { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        void Notify(string propName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        public override string ToString()
        {
            if (ArrayElement != null)
                return $"[{ArrayElementIndex}]- {ArrayElement.ToString()}";

            if (Entry is null)
                return null;

            if (Entry.NodeType == NodeType.CustomType)
                return $"{Entry.Name} - {Entry.CustomTypeName}";
            else if (Entry.NodeType == NodeType.CustomTypeArray)
                return $"{Entry.Name} - {Entry.CustomTypeName}[]";
            else if (Entry.NodeType == NodeType.RawValue)
                return $"{Entry.Name} - {(Entry as SDEFParam).RawValue.ToString()}";
            else if (Entry.NodeType == NodeType.RawValueArray)
                return $"{Entry.Name}[]";
            else
                return null;
        }
    }
}
