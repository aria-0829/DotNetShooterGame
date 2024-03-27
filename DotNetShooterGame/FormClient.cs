using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DotNetShooterGame
{
    public partial class FormClient : Form
    {
        private readonly UDPController m_udp = new UDPController();
        private PictureBox character = new PictureBox();
        private List<PictureBox> targets = new List<PictureBox>();
        private int targetsNum = 3;
        private List<Bullet> bullets = new List<Bullet>();

        public FormClient()
        {
            InitializeComponent();
            btnConnect.Enabled = true; 
            btnExit.Enabled = false;
            panelGame.Visible = false;
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            m_udp.Client("127.0.0.1", 27015);
            m_udp.isConnected = true;
            btnConnect.Enabled = false;
            btnExit.Enabled = true;
            panelGame.Visible = true;

            this.KeyPreview = true;
            this.KeyDown += FormClient_KeyDown; // Subscribe to the KeyDown event
            panelGame.MouseClick += panelGame_MouseClick; // Subscribe to the MouseClick event

            m_udp.Send("Connected");
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            if (m_udp.isConnected)
                m_udp.Close();
            this.Close();
        }

        private void AddCharacter(Point pos)
        {
            character.Image = Image.FromFile("Zombie.png");
            character.SetBounds(pos.X, pos.Y, 100, 100);
            panelGame.Controls.Add(character);
        }

        private void AddTarget(Point pos)
        {
            PictureBox target = new PictureBox();
            target.Image = Image.FromFile("Health.png");
            target.SetBounds(pos.X, pos.Y, 100, 100);
            panelGame.Controls.Add(target);
            targets.Add(target);
        }

        private void AddBullet(Vector2 pos)
        {
            Bullet bullet = new Bullet();
            bullet.pic.Image = Image.FromFile("Bomb.png");
            bullet.pic.SetBounds((int)pos.X, (int)pos.Y, 50, 50);
            panelGame.Controls.Add(bullet.pic);
            bullets.Add(bullet);
        }

        public void UpdateList()
        {
            Messages message = m_udp.GetNextMessage();
            if (m_udp.isConnected && message != null)
            {
                string[] act = message.Message.Split(':');
                switch (act[0])
                {
                    case "AddCharacter": 
                        {
                            string[] pos = act[1].Split(',');
                            Point characterPos = new Point(int.Parse(pos[0]), int.Parse(pos[1]));
                            AddCharacter(characterPos);
                            Log($"Spawn Character at {characterPos.X}, {characterPos.Y}");
                            break;
                        }
                    case "AddTargets":
                        {
                            string[] pos = act[1].Split(',');
                            for (int i = 0; i < targetsNum; i++)
                            {
                                Point targetPos = new Point(int.Parse(pos[i * 2]), int.Parse(pos[i * 2 + 1]));
                                AddTarget(targetPos);
                                Log($"Spawn Target {i} at {targetPos.X}, {targetPos.Y}");
                            }
                            break;
                        }
                    case "AddBullet":
                        {
                            string[] pos = act[1].Split(',');
                            Vector2 bulletPos = new Vector2(float.Parse(pos[0]), float.Parse(pos[1]));
                            AddBullet(bulletPos);
                            Log("Bullets Count: " + bullets.Count);
                            break;
                        }
                    case "MoveCharacter":
                        {
                            string[] pos = act[1].Split(',');
                            character.SetBounds(int.Parse(pos[0]), int.Parse(pos[1]), 100, 100);
                            break;
                        }
                    case "MoveBullet":
                        {
                            string[] pos = act[1].Split(',');
                            int i = int.Parse(pos[0]);
                            bullets[i].pic.SetBounds(int.Parse(pos[1]), int.Parse(pos[2]), 50, 50);
                            break;
                        }
                    case "RemoveTarget":
                        {
                            int index = int.Parse(act[1]);
                            panelGame.Controls.Remove(targets[index]);
                            targets.RemoveAt(index);
                            Log("Targets Count: " + targets.Count);
                            break;
                        }
                    case "RemoveBullet":
                        {
                            int index = int.Parse(act[1]);
                            panelGame.Controls.Remove(bullets[index].pic);
                            bullets.RemoveAt(index);
                            Log("Bullets Count: " + bullets.Count);
                            break;
                        }
                    case "Leave":
                        {
                            Log("Time to leave the game");
                            ResetClient();
                            break;
                        }
                }
            }
        }

        private void ResetClient()
        {
            // Reset UI
            panelGame.Controls.Clear();
            panelGame.Visible = false;
            btnConnect.Enabled = true;
            btnExit.Enabled = false;

            // Reset variables
            m_udp.isConnected = false;
            targets.Clear();
            bullets.Clear();

            //m_udp.Close();
        }

        private void panelGame_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Point mouse = panelGame.PointToClient(Cursor.Position);
                m_udp.Send("ShootTo:" + mouse.X + "," + mouse.Y);
            }
        }

        private void FormClient_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.W:
                    {
                        m_udp.Send("Up");
                        break;
                    }
                case Keys.S:
                    {
                        m_udp.Send("Down");
                        break;
                    }
                case Keys.A:
                    {
                        m_udp.Send("Left");
                        break;
                    }
                case Keys.D:
                    {
                        m_udp.Send("Right");
                        break;
                    }
            }
        }

        private void Log(object item)
        {
            listBoxClient.Items.Add(item);
        }
    }
}
