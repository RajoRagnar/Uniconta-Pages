using Uniconta.ClientTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows;
using Uniconta.Common.Utility;
using Uniconta.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.DataModel;
using System.ComponentModel.DataAnnotations;
using UnicontaClient.Controls;
using Uniconta.ClientTools.Controls;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CWGenerateInvoice : ChildWindow
    {
        public long InvoiceNumber { get; set; }
        [InputFieldData]
        [Display(Name = "Simulation", ResourceType = typeof(InputFieldDataText))]
        public bool IsSimulation { get; set; }
        [InputFieldData]
        [Display(Name = "SendInvoiceByEmail", ResourceType = typeof(InputFieldDataText))]
        public bool SendByEmail { get; set; }
        [InputFieldData]
        [Display(Name = "UpdateInventory", ResourceType = typeof(InputFieldDataText))]
        public bool UpdateInventory { get; set; }
        [InputFieldData]
        [Display(Name = "Preview", ResourceType = typeof(InputFieldDataText))]
        public bool ShowInvoice { get; set; }
        [InputFieldData]
        [Display(Name = "GenerateInvoiceOIOUBL", ResourceType = typeof(InputFieldDataText))]
        public bool GenerateOIOUBLClicked { get; set; }
        [InputFieldData]
        [Display(Name = "Date", ResourceType = typeof(InputFieldDataText))]
        public DateTime GenrateDate { get; set; }
        [InputFieldData]
        [Display(Name = "Email", ResourceType = typeof(InputFieldDataText))]
        public string Emails { get; set; }
        [InputFieldData]
        [Display(Name = "SendOnlyToThisEmail", ResourceType = typeof(InputFieldDataText))]
        public bool sendOnlyToThisEmail { get; set; }
        [InputFieldData]
        [Display(Name = "PrintImmediately", ResourceType = typeof(InputFieldDataText))]
        public bool InvoiceQuickPrint { get; set; }
        [InputFieldData]
        [Display(Name = "PostOnlyDelivered", ResourceType = typeof(InputFieldDataText))]
        public bool PostOnlyDelivered { get; set; }
        [InputFieldData]
        [Display(Name = "NumberOfCopies", ResourceType = typeof(InputFieldDataText))]
        public short NumberOfPages { get; set; } = 1;

#if !SILVERLIGHT

        protected override int DialogId { get { return DialogTableId; } }
        public int DialogTableId { get; set; }
        protected override bool ShowTableValueButton { get { return true; } }
#endif
        public CWGenerateInvoice(bool showSimulation = true, string title = "", bool showInputforInvNumber = false, bool askForEmail = false, bool showInvoice = true, bool isShowInvoiceVisible = true, bool showNoEmailMsg = false, string debtorName = "",
            bool isShowUpdateInv = false, bool isOrderOrQuickInv = false, bool isQuickPrintVisible = true, bool isDebtorOrder = false, bool InvoiceInXML = false, bool isPageCountVisible = true)
        {
            this.DataContext = this;
            InitializeComponent();
            ShowInvoice = true;
#if !SILVERLIGHT
            this.Title = Uniconta.ClientTools.Localization.lookup("GenerateInvoice");
            tbOIOUBL.Text = string.Format(Uniconta.ClientTools.Localization.lookup("CreateOBJ"), Uniconta.ClientTools.Localization.lookup("OIOUBL"));
            if (isOrderOrQuickInv)
            {
                tbOIOUBL.Visibility = Visibility.Visible;
                chkOIOUBL.Visibility = Visibility.Visible;
                chkOIOUBL.IsEnabled = true;
                chkOIOUBL.IsChecked = InvoiceInXML;
            }
#endif
            dpDate.DateTime = BasePage.GetSystemDefaultDate();


            if (!showSimulation)
            {
                this.IsSimulation = false;
                RowChk.Height = new GridLength(0);
                if (!string.IsNullOrEmpty(title))
                    this.Title = title;
            }
            if (!showInputforInvNumber)
            {
                RowInvNo.Height = new GridLength(0);
                double h = this.Height - 30;
                this.Height = h;
                txtInvNumber.Text = string.Empty;
            }
            if (!isShowUpdateInv)
            {
                RowUpdateInv.Height = new GridLength(0);
                double h = this.Height - 30;
                this.Height = h;
            }
            if (!isDebtorOrder)
            {
                RowPostOnDel.Height = new GridLength(0);
                double h = this.Height - 30;
                this.Height = h;
            }
            if (askForEmail)
            {
                if (showNoEmailMsg)
                {
                    txtNoMailMsg.Text = string.Format(Uniconta.ClientTools.Localization.lookup("DebtorHasNoEmail"), debtorName);
                    chkSendEmail.IsEnabled = false;
                }
                else
                {
                    txtNoMailMsg.Visibility = Visibility.Collapsed;
                    chkSendEmail.IsChecked = true;
                }
            }
            else
            {
                RowSendByEmail.Height = new GridLength(0);
                txtNoMailMsg.Text = string.Empty;
                txtInvNumber.Text = string.Empty;
            }

            txtInvNumber.MaxLength = 17;
            chkShowInvoice.IsChecked = showInvoice;
#if SILVERLIGHT
            Utilities.Utility.SetThemeBehaviorOnChildWindow(this);
#endif
            if (!isShowInvoiceVisible)
            {
                RowInvoice.Height = new GridLength(0);
                double h = this.Height - 30;
                this.Height = h;
            }
#if !SILVERLIGHT
            chkPrintInvoice.Visibility = isQuickPrintVisible ? Visibility.Visible : Visibility.Collapsed;
            stkPageNumberCount.Visibility = isQuickPrintVisible && isPageCountVisible ? Visibility.Visible : Visibility.Collapsed;
#endif
            this.Loaded += CW_Loaded;
        }

        void CW_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => { OKButton.Focus(); }));
        }
        private void ChildWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.DialogResult = false;
            }
            else
                if (e.Key == Key.Enter)
            {
                if (CancelButton.IsFocused)
                {
                    this.DialogResult = false;
                    return;
                }
                OKButton_Click(null, null);
            }
        }

        public void SetInvoiceNumber(long Number)
        {
            InvoiceNumber = Number;
            txtInvNumber.Text = Convert.ToString(Number);
        }

        public void SetInvoiceDate(DateTime date)
        {
            dpDate.DateTime = date;
        }

        public void SetInvPrintPreview(bool InvPrintPrvw)
        {
            chkShowInvoice.IsChecked = InvPrintPrvw;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            var str = txtInvNumber.Text;
            if (!string.IsNullOrEmpty(str) && str != "0")
            {
                if (str.Length < 18)
                    InvoiceNumber = NumberConvert.ToInt(str);
                else
                {
                    UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("NumberTooBig"), Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.OK);
                    return;
                }
            }

            SendByEmail = chkSendEmail.IsChecked.Value;
            IsSimulation = chkSimulation.IsChecked.Value;
            GenrateDate = dpDate.DateTime;
            ShowInvoice = chkShowInvoice.IsChecked.Value;
            UpdateInventory = chkUpdateInv.IsChecked.Value;
#if !SILVERLIGHT
            InvoiceQuickPrint = chkPrintInvoice.IsChecked.Value;
            GenerateOIOUBLClicked = chkOIOUBL.IsChecked.Value;
#endif
            Emails = (string.IsNullOrEmpty(txtEmail.Text) || string.IsNullOrWhiteSpace(txtEmail.Text)) ? null : txtEmail.Text;
            sendOnlyToThisEmail = chkSendOnlyEmail.IsChecked.Value;
            PostOnlyDelivered = chkPostOnlyDel.IsChecked.Value;
            this.DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

#if !SILVERLIGHT
        private void chkShowInvoice_Checked(object sender, RoutedEventArgs e)
        {
            chkPrintInvoice.IsChecked = false;
        }

        private void chkPrintInvoice_Checked(object sender, RoutedEventArgs e)
        {
            chkShowInvoice.IsChecked = false;
        }
#endif
    }
}

