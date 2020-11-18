using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Windows.Forms;
using ImpostorHQ.Plugin.Fashionable.Designer.Properties;

namespace ImpostorHQ.Plugin.Fashionable.Designer
{
    public partial class Form1 : Form
    {
        public static readonly Structures.ItemConverter Ic = new Structures.ItemConverter();
        public List<Image> Hats = new List<Image>();
        public List<Image> Clothes = new List<Image>();
        public List<Image> Pets = new List<Image>(); 
        public List<Structures.HatId> HatsStr = new List<Structures.HatId>();
        public List<Structures.SkinId> ClothesStr = new List<Structures.SkinId>();
        public List<Structures.PetId> PetsStr = new List<Structures.PetId>();

        private ushort HatIndex = 0, ClothesIndex = 0,PetIndex = 0;

        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            txtMessage.Text =
                "The resulting files must be put under the directory called \"Fashionable\", on your server's root.\nEducational purposes only. Use the plugin at your own peril.";
            for (int i = 1; i <= 94; i++)
            {
                Hats.Add((Image) Properties.Resources.ResourceManager.GetObject($"hat__{i}_"));
            }

            for (int i = 1; i <= 16; i++)
            {
                Clothes.Add((Image) Properties.Resources.ResourceManager.GetObject($"clothes__{i}_"));
            }
            for (int i = 1; i <= 11; i++)
            {
                Pets.Add((Image)Properties.Resources.ResourceManager.GetObject($"pet__{i}_"));
            }

            foreach (var hat in Ic.Hats)
            {
                cmbHat.Items.Add(hat.ToString());
                HatsStr.Add(hat);
            }

            foreach (var skin in Ic.Skins)
            {
                cmbSkin.Items.Add(skin.ToString());
                ClothesStr.Add(skin);
            }

            foreach (var pet in Ic.Pets)
            {
                cmbPets.Items.Add(pet.ToString());
                PetsStr.Add(pet);
            }
        }

        private void TriggerImmediateUpdate()
        {
            pcHat.Image = Hats[HatIndex];
            pcChl.Image = Clothes[ClothesIndex];
            pcPet.Image = Pets[PetIndex];
        }

        private void cmbHat_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.HatIndex = (ushort)cmbHat.SelectedIndex;
            TriggerImmediateUpdate();
        }

        private void cmbSkin_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.ClothesIndex = (ushort)cmbSkin.SelectedIndex;
            TriggerImmediateUpdate();
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            if (cmbHat.SelectedIndex == -1 && cmbPets.SelectedIndex == -1 && cmbSkin.SelectedIndex == -1)
            {
                MessageBox.Show("Please select something!", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "ImpostorHQ Fashion (*.hqf)|.hqf";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    var skin = new Skin()
                    {
                        Clothes = ClothesStr[ClothesIndex],
                        Hat = HatsStr[HatIndex],
                        Pet = PetsStr[PetIndex],
                        Name = Path.GetFileNameWithoutExtension(sfd.FileName)
                    };
                    File.WriteAllText(sfd.FileName.Replace(Path.GetFileNameWithoutExtension(sfd.FileName),"fashionable-" + Path.GetFileNameWithoutExtension(sfd.FileName)),skin.ToString());
                    MessageBox.Show($"File saved to: {sfd.FileName}", "Done!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void lblTitle_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void pnlBar_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void cmbPets_SelectedIndexChanged(object sender, EventArgs e)
        {
            PetIndex = (ushort)cmbPets.SelectedIndex;
            TriggerImmediateUpdate();
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }
    }
}
