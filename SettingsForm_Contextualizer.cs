using System.Text;
using System.Windows.Forms;

namespace Contextualizer
{
    internal partial class SettingsForm_Contextualizer : Form
    {


        #region Get and Set Options

        public string WordList { get; set; }
        public int WordWindowLeft { get; set; }
        public int WordWindowRight { get; set; }
        public bool CaseSensitive { get; set; }

       #endregion



        public SettingsForm_Contextualizer(string WordListInput, int WordWindowLeftInput, int WordWindowRightInput, bool ConvertToLowerCaseInput)
        {
            InitializeComponent();

            WordListTextBox.Text = WordListInput;
            WordWindowLeftTextbox.Text = WordWindowLeftInput.ToString();
            WordWindowRightTextbox.Text = WordWindowRightInput.ToString();
            CaseSensitiveCheckbox.Checked = ConvertToLowerCaseInput;

        }





        private static string windowSizeErrorMsg = "Your word window parameters must be greater than or equal to 0.";
        

        private void OKButton_Click(object sender, System.EventArgs e)
        {

            bool isNumeric = int.TryParse(WordWindowLeftTextbox.Text.Trim(), out int n);
            if (!isNumeric)
            {
                MessageBox.Show(windowSizeErrorMsg, "Parameter Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            else
            {
                if (int.Parse(WordWindowLeftTextbox.Text.Trim()) < 0)
                {
                    MessageBox.Show(windowSizeErrorMsg, "Parameter Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            isNumeric = int.TryParse(WordWindowRightTextbox.Text.Trim(), out int o);
            if (!isNumeric)
            {
                MessageBox.Show(windowSizeErrorMsg, "Parameter Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            else
            {
                if (int.Parse(WordWindowRightTextbox.Text.Trim()) < 0)
                {
                    MessageBox.Show(windowSizeErrorMsg, "Parameter Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }


            this.WordList = WordListTextBox.Text;
            this.WordWindowLeft = int.Parse(WordWindowLeftTextbox.Text.Trim());
            this.WordWindowRight = int.Parse(WordWindowRightTextbox.Text.Trim());
            this.CaseSensitive = CaseSensitiveCheckbox.Checked;




            this.DialogResult = DialogResult.OK;
        }
    }
}
