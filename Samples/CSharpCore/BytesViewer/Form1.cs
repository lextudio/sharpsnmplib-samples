using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpLib.Messaging;
using Lextm.SharpSnmpLib.Security;
using System;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;

namespace BytesViewer
{
    public partial class Form1 : Form
    {
        private const string TempFilePath = "userSettings.dat";

        public Form1()
        {
            InitializeComponent();
            tscbAuthentication.SelectedIndex = 0;
            tscbPrivacy.SelectedIndex = 0;

            LoadUserSettings();
        }

        private void txtBytes_TextChanged(object sender, EventArgs e)
        {
            tvMessage.Nodes.Clear();
            var users = new UserRegistry();
            IAuthenticationProvider authen;
            if (tscbAuthentication.SelectedIndex == 0)
            {
                authen = DefaultAuthenticationProvider.Instance;
            }
            else if (tscbAuthentication.SelectedIndex == 1)
            {
                authen = new MD5AuthenticationProvider(new OctetString(tstxtAuthentication.Text));
            }
            else
            {
                authen = new SHA1AuthenticationProvider(new OctetString(tstxtAuthentication.Text));
            }

            IPrivacyProvider privacy;
            if (tscbPrivacy.SelectedIndex == 0)
            {
                privacy = new DefaultPrivacyProvider(authen);
            }
            else if (tscbPrivacy.SelectedIndex == 1)
            {
                privacy = new DESPrivacyProvider(new OctetString(tstxtPrivacy.Text), authen);
            }
            else
            {
                privacy = new AESPrivacyProvider(new OctetString(tstxtPrivacy.Text), authen);
            }

            users.Add(new User(new OctetString(tstxtUser.Text), privacy));

            try
            {
                var messages = MessageFactory.ParseMessages(ByteTool.Convert(txtBytes.Text.Replace("\"", null).Replace("+", null)), users);
                messages.Fill(tvMessage);
            }
            catch (Exception ex)
            {
                tvMessage.Nodes.Add(ex.Message);
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveUserSettings();
        }

        private void SaveUserSettings()
        {
            var settings = new UserSettings
            {
                AuthenticationIndex = tscbAuthentication.SelectedIndex,
                PrivacyIndex = tscbPrivacy.SelectedIndex,
                AuthenticationText = tstxtAuthentication.Text,
                PrivacyText = tstxtPrivacy.Text,
                UserText = tstxtUser.Text,
                Input = txtBytes.Text
            };

            var json = JsonSerializer.Serialize(settings);
            File.WriteAllText(TempFilePath, json);
        }

        private void LoadUserSettings()
        {
            if (File.Exists(TempFilePath))
            {
                var json = File.ReadAllText(TempFilePath);
                var settings = JsonSerializer.Deserialize<UserSettings>(json);

                tscbAuthentication.SelectedIndex = settings.AuthenticationIndex;
                tscbPrivacy.SelectedIndex = settings.PrivacyIndex;
                tstxtAuthentication.Text = settings.AuthenticationText;
                tstxtPrivacy.Text = settings.PrivacyText;
                tstxtUser.Text = settings.UserText;
                txtBytes.Text = settings.Input;
            }
        }
    }

    [Serializable]
    public class UserSettings
    {
        public int AuthenticationIndex { get; set; }
        public int PrivacyIndex { get; set; }
        public string AuthenticationText { get; set; }
        public string PrivacyText { get; set; }
        public string UserText { get; set; }

        public string Input { get; set; }
    }
}
