using UnicontaClient.Models;
using UnicontaClient.Pages;
using UnicontaClient.Utilities;
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
using System.Windows.Shapes;
using Uniconta.API.Service;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class DebtorOfferLineReportGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(DebtorOfferLineClient); } }
        public override bool Readonly { get { return false; } }
    }
    /// <summary>
    /// Interaction logic for DebtorOfferLineReport.xaml
    /// </summary>
    public partial class DebtorOfferLineReport : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.DebtorOfferLineReport ; } }

        SQLCache items, warehouse;
        public DebtorOfferLineReport(UnicontaBaseEntity masterRecord) : base(masterRecord)
        {
            InitializeComponent();
            dgDebtorOfferLineGrid.UpdateMaster(masterRecord);
            InitPage();
        }

        public DebtorOfferLineReport(BaseAPI API) : base(API, string.Empty)
        {
            InitializeComponent();
            InitPage();
        }

        private void InitPage()
        {
            dgDebtorOfferLineGrid.tableView.ShowGroupPanel = true;
            SetRibbonControl(localMenu, dgDebtorOfferLineGrid);
            dgDebtorOfferLineGrid.api = api;
            dgDebtorOfferLineGrid.BusyIndicator = busyIndicator;
            dgDebtorOfferLineGrid.ShowTotalSummary();
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
            dgDebtorOfferLineGrid.View.DataControl.CurrentItemChanged += DataControl_CurrentItemChanged;
            InitialLoad();
            SetColumns();
        }

        private void InitialLoad()
        {
            var comp = api.CompanyEntity;
            items = comp.GetCache(typeof(InvItem));
            warehouse = comp.GetCache(typeof(InvWarehouse));
            if (comp.UnitConversion)
                Unit.Visible = true;
        }

        private void DataControl_CurrentItemChanged(object sender, DevExpress.Xpf.Grid.CurrentItemChangedEventArgs e)
        {
            DebtorOfferLineClient oldselectedItem = e.OldItem as DebtorOfferLineClient;
            if (oldselectedItem != null)
                oldselectedItem.PropertyChanged -= DebtorOfferLineClient_PropertyChanged;

            DebtorOfferLineClient selectedItem = e.NewItem as DebtorOfferLineClient;
            if (selectedItem != null)
                selectedItem.PropertyChanged += DebtorOfferLineClient_PropertyChanged; ;
        }

        private void DebtorOfferLineClient_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var rec = (DebtorOfferLineClient)sender;
            switch (e.PropertyName)
            {
                case "Item":
                    var selectedItem = (InvItem)items.Get(rec._Item);
                    if (selectedItem != null)
                    {
                        rec.Text = selectedItem._Name;
                        rec.NotifyPropertyChanged("Text");
                        TableField.SetUserFieldsFromRecord(selectedItem, rec);
                        if (selectedItem._Blocked)
                            UtilDisplay.ShowErrorCode(ErrorCodes.ItemIsOnHold, null);
                    }
                    break;
                case "Warehouse":
                    if (warehouse != null)
                    {
                        var selected = (InvWarehouse)warehouse.Get(rec._Warehouse);
                        setLocation(selected, (DebtorOfferLineClient)rec);
                    }
                    break;
                case "Location":
                    if (string.IsNullOrEmpty(rec._Warehouse))
                        rec._Location = null;
                    break;
            }
        }

        async void setLocation(InvWarehouse master, DebtorOfferLineClient rec)
        {
            if (api.CompanyEntity.Location)
            {
                if (master != null)
                    rec.locationSource = master.Locations ?? await master.LoadLocations(api);
                else
                {
                    rec.locationSource = null;
                    rec.Location = null;
                }
                rec.NotifyPropertyChanged("LocationSource");
            }
        }


        private void LocalMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgDebtorOfferLineGrid.SelectedItem as DebtorOfferLineClient;
            switch (ActionType)
            {
                case "SaveGrid":
                    dgDebtorOfferLineGrid.SaveData();
                    break;
                case "DeleteRow":
                    if (selectedItem != null)
                        dgDebtorOfferLineGrid.DeleteRow();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        private void SetColumns()
        {
            var company = api.CompanyEntity;
            if (!company.Location || !company.Warehouse)
                Location.Visible = Location.ShowInColumnChooser = false;
            if (!company.Warehouse)
                Warehouse.Visible = Warehouse.ShowInColumnChooser = false;
            Utility.SetupVariants(api, colVariant, colVariant1, colVariant2, colVariant3, colVariant4, colVariant5, Variant1Name, Variant2Name, Variant3Name, Variant4Name, Variant5Name);
        }
        void setDim()
        {
            Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
        }

        CorasauGridLookupEditorClient prevLocation;
        private void Location_GotFocus(object sender, RoutedEventArgs e)
        {
            var selectedItem = dgDebtorOfferLineGrid.SelectedItem as DebtorOfferLineClient;
            if (selectedItem != null && selectedItem._Warehouse != null && warehouse != null)
            {
                var selected = (InvWarehouse)warehouse.Get(selectedItem._Warehouse);
                setLocation(selected, selectedItem);
                if (prevLocation != null)
                    prevLocation.isValidate = false;
                var editor = (CorasauGridLookupEditorClient)sender;
                prevLocation = editor;
                editor.isValidate = true;
            }
        }
        CorasauGridLookupEditorClient prevVariant1;
        private void variant1_GotFocus(object sender, RoutedEventArgs e)
        {
            if (prevVariant1 != null)
                prevVariant1.isValidate = false;
            var editor = (CorasauGridLookupEditorClient)sender;
            prevVariant1 = editor;
            editor.isValidate = true;
        }

        CorasauGridLookupEditorClient prevVariant2;
        private void variant2_GotFocus(object sender, RoutedEventArgs e)
        {
            if (prevVariant2 != null)
                prevVariant2.isValidate = false;
            var editor = (CorasauGridLookupEditorClient)sender;
            prevVariant2 = editor;
            editor.isValidate = true;
        }

        private void SerieBatch_GotFocus(object sender, RoutedEventArgs e)
        {
            var selItem = dgDebtorOfferLineGrid.SelectedItem as DebtorOfferLineClient;
            if (selItem == null || string.IsNullOrEmpty(selItem._Item))
                return;
            setSerieBatchSource(selItem);
        }

        async void setSerieBatchSource(DebtorOfferLineClient row)
        {
            var cache = api.CompanyEntity.GetCache(typeof(InvItem));
            var invItemMaster = cache.Get(row._Item) as InvItem;
            if (invItemMaster == null)
                return;
            if (row.SerieBatches != null && row.SerieBatches.First()._Item == row._Item)/*Bind if Item changed*/
                return;
            List<UnicontaBaseEntity> masters = null;
            if (row._Qty < 0)
            {
                masters = new List<UnicontaBaseEntity>() { invItemMaster };
            }
            else
            {
                // We only select opens
                var mast = new InvSerieBatchOpen();
                mast.SetMaster(invItemMaster);
                masters = new List<UnicontaBaseEntity>() { mast };
            }
            var res = await api.Query<SerialToOrderLineClient>(masters, null);
            if (res != null && res.Length > 0)
            {
                row.SerieBatches = res;
                row.NotifyPropertyChanged("SerieBatches");
            }
        }

        protected override async void LoadCacheInBackGround()
        {
            var api = this.api;
            var Comp = api.CompanyEntity;
            if (this.items == null)
                this.items = await Comp.LoadCache(typeof(InvItem), api).ConfigureAwait(false);

            if (Comp.Warehouse && this.warehouse == null)
                this.warehouse = await Comp.LoadCache(typeof(InvWarehouse), api).ConfigureAwait(false);

            await Comp.LoadCache(typeof(Debtor), api).ConfigureAwait(false);
        }
    }
}
