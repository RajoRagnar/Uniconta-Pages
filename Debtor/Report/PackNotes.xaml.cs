using Uniconta.API.DebtorCreditor;
using Uniconta.API.Service;
using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using UnicontaClient.Controls.Dialogs;
using Uniconta.ClientTools.Controls;
using DevExpress.XtraReports.UI;
using System.ComponentModel.DataAnnotations;
using UnicontaClient.Pages;

#if !SILVERLIGHT
using ubl_norway_uniconta;
using Microsoft.Win32;
using UnicontaClient.Pages;
#endif

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class DebtorDeliveryNoteLocal : DebtorDeliveryNoteClient
    {
        [Display(Name = "System Info")]
        public string SystemInfo { get { return _SystemInfo; } }
        public string _SystemInfo;

        internal void NotifySystemInfoSet()
        {
            NotifyPropertyChanged("SystemInfo");
        }
    }

    public class PackNotesGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(DebtorDeliveryNoteLocal); } }
        public override IComparer GridSorting { get { return new DCInvoiceSort(); } }
    }

    public partial class PackNotes : GridBasePage
    {
        SQLCache Debcache;
        public PackNotes(BaseAPI API) : base(API, string.Empty)
        {
            InitPage();
        }
        public PackNotes(SynchronizeEntity syncEntity)
            : base(syncEntity, true)
        {
            this.syncEntity = syncEntity;
            InitPage(syncEntity.Row);
            SetHeader();
        }
        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            dgPackNotesGrid.UpdateMaster(args);
            SetHeader();
            InitQuery();
        }
        void SetHeader()
        {
            var masterClient = dgPackNotesGrid.masterRecord as Debtor;
            if (masterClient == null)
                return;
            string header = string.Format("{0}/{1}", Uniconta.ClientTools.Localization.lookup("Packnote"), masterClient._Account);
            SetHeader(header);
        }
        public PackNotes(UnicontaBaseEntity master)
            : base(master)
        {
            InitPage(master);
        }

        private void InitPage(UnicontaBaseEntity master = null)
        {
            InitializeComponent();
            dgPackNotesGrid.UpdateMaster(master);
            SetRibbonControl(localMenu, dgPackNotesGrid);
            dgPackNotesGrid.api = api;
            dgPackNotesGrid.BusyIndicator = busyIndicator;
            dgPackNotesGrid.api = api;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
#if SILVERLIGHT
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            UtilDisplay.RemoveMenuCommand(rb, "GenerateOioXml");
#endif
            initialLoad();
            dgPackNotesGrid.ShowTotalSummary();
            var Comp = api.CompanyEntity;
            if (Comp.RoundTo100)
                CostValue.HasDecimals = NetAmount.HasDecimals = TotalAmount.HasDecimals = Margin.HasDecimals = SalesValue.HasDecimals = false;
        }

        async void initialLoad()
        {
            var Comp = api.CompanyEntity;
            Debcache = Comp.GetCache(typeof(Debtor)) ?? await Comp.LoadCache(typeof(Debtor), api);
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            bool showFields = (dgPackNotesGrid.masterRecords == null);
            Account.Visible = showFields;
            Name.Visible = showFields;
            setDim();
            if (!api.CompanyEntity.DeliveryAddress)
            {
                DeliveryName.Visible = false;
                DeliveryAddress1.Visible = false;
                DeliveryAddress2.Visible = false;
                DeliveryAddress3.Visible = false;
                DeliveryZipCode.Visible = false;
                DeliveryCity.Visible = false;
                DeliveryCountry.Visible = false;
            }
        }

        public async override Task InitQuery()
        {
            await dgPackNotesGrid.Filter(null);

            var api = this.api;
            if (api.CompanyEntity.DeliveryAddress)
            {
                var lst = dgPackNotesGrid.ItemsSource as IEnumerable<DebtorDeliveryNoteLocal>;
                if (lst != null)
                {
                    foreach (var rec in lst)
                    {
                        DebtorOrders.SetDeliveryAdress(rec, rec.Debtor, api);
                    }
                }
            }
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgPackNotesGrid.SelectedItem as DebtorDeliveryNoteLocal;
            string salesHeader = string.Empty;
            if (selectedItem != null)
                salesHeader = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Orders"), selectedItem._OrderNumber);
            switch (ActionType)
            {
                case "DeliveryNoteLine":
                    if (selectedItem != null)
                        AddDockItem(TabControls.DebtorInvoiceLines, dgPackNotesGrid.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("PackNoteNumber"), selectedItem._InvoiceNumber));
                    break;
                case "ShowDeliveryNote":
                    if (selectedItem == null || dgPackNotesGrid.SelectedItems == null)
                        return;
                    var selectedItems = dgPackNotesGrid.SelectedItems.Cast<DebtorDeliveryNoteLocal>();
                    ShowDeliveryNote(selectedItems);
                    break;
                case "SendDeliveryNote":
                    if (selectedItem != null)
                        SendDeliveryNote(selectedItem);
                    break;
                case "AddDoc":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserDocsPage, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Documents"), selectedItem._InvoiceNumber));
                    break;
                case "RefreshGrid":
                    InitQuery();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        async private void ShowDeliveryNote(IEnumerable<DebtorDeliveryNoteLocal> debtorInvoices)
        {
            busyIndicator.IsBusy = true;
            busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("GeneratingPage");

            try
            {
                var invoicesList = debtorInvoices.ToList();
#if !SILVERLIGHT
                var failedPrints = new List<long>();
                var invoiceReports = new List<IPrintReport>();
                long invNumber = 0;
#elif SILVERLIGHT
                int top = 200, left = 300;
                int count = invoicesList.Count();

                if (count > 1)
                {
#endif
                foreach (var debtInvoice in invoicesList)
                {
#if !SILVERLIGHT
                    IPrintReport printReport = await PrintInvoice(debtInvoice);
                    invNumber = debtInvoice._InvoiceNumber;
                    if (printReport?.Report != null)
                        invoiceReports.Add(printReport);
                    else
                        failedPrints.Add(debtInvoice.InvoiceNumber);
                }

                if (invoiceReports.Count() > 0)
                {
                    if (invoiceReports.Count == 1 && invNumber > 0)
                    {
                        var reportName = string.Format("{0}_{1}", Uniconta.ClientTools.Localization.lookup("PackNote"), invNumber);
                        var dockName = string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("Preview"), string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("PackNote"), invNumber));
                        AddDockItem(TabControls.StandardPrintReportPage, new object[] { invoiceReports, reportName }, dockName);
                    }
                    else
                    {
                        var reportsName = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("ShowPrint"), Uniconta.ClientTools.Localization.lookup("PackNotes"));
                        AddDockItem(TabControls.StandardPrintReportPage, new object[] { invoiceReports, Uniconta.ClientTools.Localization.lookup("PackNotes") }, reportsName);
                    }

                    if (failedPrints.Count > 0)
                    {
                        var failedList = string.Join(",", failedPrints);
                        UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("FailedPrintmsg") + failedList, Uniconta.ClientTools.Localization.lookup("Error"), MessageBoxButton.OK);
                    }
                }
                else
                    UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("LayoutDoesNotExist"), Uniconta.ClientTools.Localization.lookup("PackNote")),
                       Uniconta.ClientTools.Localization.lookup("Error"), MessageBoxButton.OK);
#elif SILVERLIGHT
                        DefaultPrint(debtInvoice, true, new Point(top, left));
                        left = left - left / count;
                        top = top - top / count;
                    }
                }
                else
                    DefaultPrint(debtorInvoices.Single());
#endif
            }
            catch (Exception ex)
            {
                busyIndicator.IsBusy = false;
                api.ReportException(ex, string.Format("PackNotes.ShowInvoice(), CompanyId={0}", api.CompanyId));
            }
            finally { busyIndicator.IsBusy = false; }
        }

#if !SILVERLIGHT
        private async Task<IPrintReport> PrintInvoice(DebtorInvoiceClient debtorInvoice)
        {
            IPrintReport iprintReport = null;

            var debtorQcpPrint = new UnicontaClient.Pages.DebtorInvoicePrintReport(debtorInvoice, api, CompanyLayoutType.Packnote);
            var isInitializedSuccess = await debtorQcpPrint.InstantiateFields();
            if (isInitializedSuccess)
            {
                var standardDebtorPackNote = new DebtorQCPReportClient(debtorQcpPrint.Company, debtorQcpPrint.Debtor, debtorQcpPrint.DebtorInvoice, debtorQcpPrint.InvTransInvoiceLines, null,
                    debtorQcpPrint.CompanyLogo, debtorQcpPrint.ReportName, (byte)Uniconta.ClientTools.Controls.Reporting.StandardReports.PackNote, messageClient: debtorQcpPrint.MessageClient);

                var standardReports = new IDebtorStandardReport[] { standardDebtorPackNote };
                iprintReport = new StandardPrintReport(api, standardReports, (byte)Uniconta.ClientTools.Controls.Reporting.StandardReports.PackNote);
                await iprintReport.InitializePrint();


                if (iprintReport?.Report == null)
                {
                    iprintReport = new LayoutPrintReport(api, debtorInvoice, CompanyLayoutType.Packnote);
                    await iprintReport.InitializePrint();
                }
            }
            return iprintReport;
        }

#elif SILVERLIGHT
        private void DefaultPrint(DebtorInvoiceClient debtorInvoice)
        {
            object[] ob = new object[2];
            ob[0] = debtorInvoice;
            ob[1] = CompanyLayoutType.Packnote;
            string headerName = "PackNote";
            AddDockItem(TabControls.ProformaInvoice, ob, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup(headerName),
                debtorInvoice._InvoiceNumber));
        }

        private void DefaultPrint(DebtorInvoiceClient debtorInvoice, bool isFloat, Point position, bool isPackNote = false)
        {
            object[] ob = new object[2];
            ob[0] = debtorInvoice;
            ob[1] = CompanyLayoutType.Packnote;
            string headerName = "PackNote";
            AddDockItem(TabControls.ProformaInvoice, ob, isFloat, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup(headerName),
                debtorInvoice._InvoiceNumber), floatingLoc: position);
        }
#endif

        private void ShowOrderLines(DCOrder order)
        {
            var confrimationText = string.Format(" {0}. {1}:{2},{3}:{4}\r\n{5}", Uniconta.ClientTools.Localization.lookup("SalesOrderCreated"), Uniconta.ClientTools.Localization.lookup("OrderNumber"), order._OrderNumber,
                Uniconta.ClientTools.Localization.lookup("Account"), order._DCAccount, string.Concat(string.Format(Uniconta.ClientTools.Localization.lookup("GoTo"), Uniconta.ClientTools.Localization.lookup("Orderline")), " ?"));

            var confirmationBox = new CWConfirmationBox(confrimationText, string.Empty, false);
            confirmationBox.Closing += delegate
            {
                if (confirmationBox.DialogResult == null)
                    return;

                switch (confirmationBox.ConfirmationResult)
                {
                    case CWConfirmationBox.ConfirmationResultEnum.Yes:
                        AddDockItem(TabControls.DebtorOrderLines, order, string.Format("{0}:{1},{2}", Uniconta.ClientTools.Localization.lookup("OrdersLine"), order._OrderNumber, ClientHelper.GetName(order.CompanyId, typeof(Debtor), order._DCAccount)));
                        break;

                    case CWConfirmationBox.ConfirmationResultEnum.No:
                        break;
                }
            };
            confirmationBox.Show();

        }
        void SendDeliveryNote(DebtorInvoiceClient invClient)
        {
            UnicontaClient.Pages.CWSendInvoice cwSendInvoice = new UnicontaClient.Pages.CWSendInvoice();
#if !SILVERLIGHT
            cwSendInvoice.DialogTableId = 2000000029;
#endif
            cwSendInvoice.Closed += async delegate
            {
                if (cwSendInvoice.DialogResult == true)
                {
                    busyIndicator.IsBusy = true;
                    InvoiceAPI Invapi = new InvoiceAPI(api);
                    ErrorCodes res = await Invapi.SendInvoice(invClient, cwSendInvoice.Emails, cwSendInvoice.sendOnlyToThisEmail);

                    busyIndicator.IsBusy = false;
                    if (res == ErrorCodes.Succes)
                        UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("SendEmailMsgOBJ"), Uniconta.ClientTools.Localization.lookup("PackNote")), Uniconta.ClientTools.Localization.lookup("Message"));
                    else
                        UtilDisplay.ShowErrorCode(res);
                }
            };
            cwSendInvoice.Show();
        }

        protected override void LoadCacheInBackGround()
        {
            var Comp = api.CompanyEntity;

            var lst = new List<Type>() { typeof(Uniconta.DataModel.InvItem), typeof(Uniconta.DataModel.PaymentTerm) };
            if (Comp.ItemVariants)
            {
                lst.Add(typeof(Uniconta.DataModel.InvVariant1));
                lst.Add(typeof(Uniconta.DataModel.InvVariant2));
                var n = Comp.NumberOfVariants;
                if (n >= 3)
                    lst.Add(typeof(Uniconta.DataModel.InvVariant3));
                if (n >= 4)
                    lst.Add(typeof(Uniconta.DataModel.InvVariant4));
                if (n >= 5)
                    lst.Add(typeof(Uniconta.DataModel.InvVariant5));
            }
            if (Comp.Warehouse)
                lst.Add(typeof(Uniconta.DataModel.InvWarehouse));
            if (Comp.Shipments)
            {
                lst.Add(typeof(Uniconta.DataModel.ShipmentType));
                lst.Add(typeof(Uniconta.DataModel.DeliveryTerm));
            }
            if (Comp.DeliveryAddress)
                lst.Add(typeof(Uniconta.DataModel.WorkInstallation));

            LoadType(lst);
        }

        void setDim()
        {
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
        }
    }
}
