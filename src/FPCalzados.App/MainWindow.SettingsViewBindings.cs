using System.Windows.Controls;

namespace FPCalzados.App;

public partial class MainWindow
{
    private TextBox RuleNameBox => SettingsViewControl.RuleNameBox;
    private ComboBox RuleMethodBox => SettingsViewControl.RuleMethodBox;
    private TextBox RulePercentBox => SettingsViewControl.RulePercentBox;
    private TextBox RuleInstallmentsBox => SettingsViewControl.RuleInstallmentsBox;
    private DataGrid RulesGrid => SettingsViewControl.RulesGrid;
    private TextBox BusinessNameBox => SettingsViewControl.BusinessNameBox;
    private TextBox BusinessAddressBox => SettingsViewControl.BusinessAddressBox;
    private TextBox BusinessPhoneBox => SettingsViewControl.BusinessPhoneBox;
    private TextBox BusinessTaxIdBox => SettingsViewControl.BusinessTaxIdBox;
    private TextBox BusinessTicketFooterBox => SettingsViewControl.BusinessTicketFooterBox;
    private ComboBox PasswordRoleBox => SettingsViewControl.PasswordRoleBox;
    private PasswordBox NewPasswordBox => SettingsViewControl.NewPasswordBox;
    private DataGrid AuditLogsGrid => SettingsViewControl.AuditLogsGrid;

    private void WireSettingsViewEvents()
    {
        SettingsViewControl.NewRuleButton.Click += NewRule_Click;
        SettingsViewControl.SaveRuleButton.Click += SaveRule_Click;
        SettingsViewControl.DeleteRuleButton.Click += DeleteRule_Click;
        SettingsViewControl.ChangePasswordButton.Click += ChangePassword_Click;
        SettingsViewControl.SaveBusinessSettingsButton.Click += SaveBusinessSettings_Click;
        SettingsViewControl.RefreshAuditLogsButton.Click += RefreshAuditLogs_Click;
        SettingsViewControl.ExportAuditLogsButton.Click += ExportAuditLogs_Click;
        SettingsViewControl.ReviewDuplicateCustomersButton.Click += ReviewDuplicateCustomers_Click;
        SettingsViewControl.RulesGrid.SelectionChanged += RulesGrid_SelectionChanged;
    }
}
