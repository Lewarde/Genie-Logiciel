using System;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace CryptoSoft
{
    public partial class Form1 : Form
    {
        private Button selectFileSourceButton;
        private Button selectFileTargetButton;
        private Button nextButton;
        private TextBox fileSourceTextBox;
        private TextBox fileTargetTextBox;
        private Cryptage cryptage;

        public Form1(string? sourcePath, string? targetPath)
        {
            InitializeCustomComponents();

            if (!string.IsNullOrEmpty(sourcePath))
                fileSourceTextBox.Text = sourcePath;

            if (!string.IsNullOrEmpty(targetPath))
                fileTargetTextBox.Text = targetPath;
         

                AutoEncrypt();
            
        }


        private void InitializeCustomComponents()
        {
            // Source
            fileSourceTextBox = new TextBox
            {
                Location = new System.Drawing.Point(30, 30),
                Width = 300,
                ReadOnly = true
            };
            this.Controls.Add(fileSourceTextBox);

            selectFileSourceButton = new Button
            {
                Text = "...",
                Location = new System.Drawing.Point(330, 30)
            };
            selectFileSourceButton.Click += SelectFileSourceButton_Click;
            this.Controls.Add(selectFileSourceButton);

            // Target
            fileTargetTextBox = new TextBox
            {
                Location = new System.Drawing.Point(30, 70),
                Width = 300,
                ReadOnly = true
            };
            this.Controls.Add(fileTargetTextBox);

            selectFileTargetButton = new Button
            {
                Text = "...",
                Location = new System.Drawing.Point(330, 70)
            };
            selectFileTargetButton.Click += SelectFileTargetButton_Click;
            this.Controls.Add(selectFileTargetButton);

            //// Suivant
            nextButton = new Button
            {
                Text = "Suivant",
                Location = new System.Drawing.Point(330, 110)
            };
            nextButton.Click += NextButton_Click;
            this.Controls.Add(nextButton);

            this.Text = "Sélecteur de fichier texte";
            this.Size = new System.Drawing.Size(500, 200);
            this.StartPosition = FormStartPosition.CenterScreen;
        }

        private void AutoEncrypt()
        {
            try
            {
                string key = "A"; 
                string text = File.ReadAllText(fileSourceTextBox.Text, Encoding.UTF8);
                cryptage = new Cryptage();
                string encrypted = cryptage.Encrypt(text, key);
                File.WriteAllText(fileTargetTextBox.Text, encrypted, Encoding.UTF8);
                MessageBox.Show("Cryptage automatique terminé avec succès !", "Succès", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors du cryptage automatique : " + ex.Message);
                this.Close();
            }
        }

        private void SelectFileSourceButton_Click(object sender, EventArgs e)
        {
            using OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Sélectionner un fichier texte";
            //openFileDialog.Filter = "Fichiers texte (*.txt)|*.txt|Tous les fichiers (*.*)|*.*";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                fileSourceTextBox.Text = openFileDialog.FileName;
                fileTargetTextBox.Text = openFileDialog.FileName + ".crypt";
            }
        }

        private void SelectFileTargetButton_Click(object sender, EventArgs e)
        {
            using SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Title = "Enregistrer le fichier crypté";
            //saveFileDialog.Filter = "Fichiers cryptés (*.crypt)|*.crypt|Tous les fichiers (*.*)|*.*";
            saveFileDialog.FileName = Path.GetFileName(fileSourceTextBox.Text) + ".crypt";

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                fileTargetTextBox.Text = saveFileDialog.FileName;
            }
        }

        private void NextButton_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(fileSourceTextBox.Text) || string.IsNullOrWhiteSpace(fileTargetTextBox.Text))
            {
                MessageBox.Show("Veuillez sélectionner un fichier source et une destination.");
                return;
            }

            try
            {
                string key = "A"; // Une lettre comme clé (à améliorer si besoin)
                string text = File.ReadAllText(fileSourceTextBox.Text, Encoding.UTF8);
                cryptage = new Cryptage();
                string encrypted = cryptage.Encrypt(text, key);
                File.WriteAllText(fileTargetTextBox.Text, encrypted, Encoding.UTF8);
                Application.Exit();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors du cryptage : " + ex.Message);
                Application.Exit();
            }
        }
    }
}
