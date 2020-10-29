using System;
using System.Collections.Generic;
using System.Linq;
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
        public StandardDefinition Database { get; set; }
        public SDEFListing CurrentParameter { get; set; }
        public string LastFile { get; set; }
        public MainWindow()
        {
            InitializeComponent();
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
                    Database = SDEFData.FromFile(openDialog.FileName);
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
            }
            else 
            {
                SDEFVariant value = CurrentParameter?.Entry?.NodeType == NodeType.RawValue ? CurrentParameter.Entry.RawValue : CurrentParameter.ArrayElement;
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
                    default:
                        break;
                }
            }
            
        }

        private void param_Double_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (param_Double.Value != null)
            {
                CurrentParameter.Entry.RawValue.Set(param_Double.Value.Value);
                CurrentParameter.Name = CurrentParameter.ToString();
            }
        }

        private void param_Single_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (param_Single.Value != null)
            {
                var val = CurrentParameter.ArrayElement ?? CurrentParameter.Entry.RawValue;
                val.Set(param_Single.Value.Value);
                CurrentParameter.Name = CurrentParameter.ToString();
            }
        }

        private void param_UInteger_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (param_UInteger.Value != null)
            {
                var val = CurrentParameter.ArrayElement ?? CurrentParameter.Entry.RawValue;
                val.Set(param_UInteger.Value.Value);
                CurrentParameter.Name = CurrentParameter.ToString();
            }
        }

        private void param_SByte_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (param_SByte.Value != null)
            {
                var val = CurrentParameter.ArrayElement ?? CurrentParameter.Entry.RawValue;
                val.Set(param_SByte.Value.Value);
                CurrentParameter.Name = CurrentParameter.ToString();
            }
        }

        private void param_Byte_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (param_Byte.Value != null)
            {
                var val = CurrentParameter.ArrayElement ?? CurrentParameter.Entry.RawValue;
                val.Set(param_Byte.Value.Value);
                CurrentParameter.Name = CurrentParameter.ToString();
            }
        }

        private void param_Integer_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (param_Integer.Value != null)
            {
                var val = CurrentParameter.ArrayElement ?? CurrentParameter.Entry.RawValue;
                val.Set(param_Integer.Value.Value);
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

        public void BuildChildTree(SDEFListing item, SDEFParameter current)
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
                    BuildChildTree(listing, entry);
                }
                else if (entry.NodeType == NodeType.RawValueArray)
                {
                    foreach (var i in entry.RawValuesArray)
                    {
                        var arrValueListing = new SDEFListing() { Name = $"(value) - {i.ToString()}" };
                        arrValueListing.ArrayElement = i;
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

        public SDEFParameter Entry { get; set; }

        public ObservableCollection<SDEFListing> Items { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        void Notify(string propName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        public override string ToString()
        {
            if (ArrayElement != null)
                return $"(value) - {ArrayElement.ToString()}";

            if (Entry.NodeType == NodeType.CustomType)
                return $"{Entry.Name} - {Entry.CustomTypeName}";
            else if (Entry.NodeType == NodeType.CustomTypeArray)
                return $"{Entry.Name} - {Entry.CustomTypeName}[]";
            else if (Entry.NodeType == NodeType.RawValue)
                return $"{Entry.Name} - {Entry.RawValue.ToString()}";
            else if (Entry.NodeType == NodeType.RawValueArray)
                return $"{Entry.Name}[]";
            else
                return null;
        }
    }
}
