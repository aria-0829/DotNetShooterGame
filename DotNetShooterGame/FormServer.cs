using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DotNetShooterGame
{
    public partial class FormServer : Form
    {
        private readonly UDPController m_udp = new UDPController();
        private PictureBox character = new PictureBox();
        private List<PictureBox> targets = new List<PictureBox>();
        private List<PictureBox> targetsToRemove = new List<PictureBox>();
        private int targetsNum = 3;
        private List<Bullet> bullets = new List<Bullet>();
        private List<Bullet> bulletsToRemove = new List<Bullet>();
        private DateTime lastFrameTime = DateTime.Now;
        private int frameCounter = 0;

        public FormServer()
        {
            InitializeComponent();
            m_udp.Server("127.0.0.1", 27015);
            panelGame.Visible = false;
        }

        private void AddCharacter()
        {
            character.Image = Image.FromFile("Zombie.png");
            Random r = new Random();
            character.SetBounds(r.Next(panelGame.Width / 2, panelGame.Width - 100),  
                                r.Next(panelGame.Width / 2, panelGame.Height - 100), 100, 100); // Spawn the character in a random position between the center and the bottom right corner
            character.BackColor = Color.Transparent;
            panelGame.Controls.Add(character);
        }

        private void AddTargets()
        {
            Random r = new Random();

            for (int i = 0; i < targetsNum; i++)
            {
                PictureBox target = new PictureBox();
                target.Image = Image.FromFile("Health.png"); 
                target.SetBounds(r.Next(0, panelGame.Width - 100), 
                                 r.Next(0, panelGame.Height - 100), 100, 100);
                panelGame.Controls.Add(target);
                targets.Add(target);
            }
        }

        private void Shoot(Point mouse)
        {
            // Add PictureBox
            Bullet bullet = new Bullet();
            bullet.pic.Image = Image.FromFile("Bomb.png");
            bullet.pic.SetBounds(character.Bounds.X, character.Bounds.Y, 50, 50); // Set the bullet position to the character's position
            panelGame.Controls.Add(bullet.pic);

            // Set bullet
            bullet.position = new Vector2(character.Bounds.X, character.Bounds.Y);
            Vector2 direction = new Vector2(mouse.X - character.Bounds.X, mouse.Y - character.Bounds.Y);
            bullet.direction = Vector2.Normalize(direction);
            bullets.Add(bullet);

            m_udp.SendTo("AddBullet:" + bullet.pic.Bounds.X + "," + bullet.pic.Bounds.Y, m_udp.m_epFrom);
        }

        private void UpdateGame()
        {
            MoveBullets();
            DetectCollision();
            RemoveDestroyedObjects();

            // Send bullet positions to client
            frameCounter++;
            int SendInterval = 200;
            if (frameCounter % SendInterval == 0)
            {
                for (int i = 0; i < bullets.Count; i++)
                {
                    m_udp.SendTo("MoveBullet:" + i + "," + bullets[i].pic.Bounds.X + "," + 
                                 bullets[i].pic.Bounds.Y, m_udp.m_epFrom);
                }
            }

            // Check if there are no more targets
            if (m_udp.isConnected == true && targets.Count == 0)
            {
                m_udp.SendTo("Leave:", m_udp.m_epFrom);
                ResetServer();
            }
        }

        private void MoveBullets()
        {
            DateTime currentTime = DateTime.Now;
            double deltaTime = (currentTime - lastFrameTime).TotalSeconds;
            lastFrameTime = currentTime;

            foreach (Bullet bullet in bullets)
            {
                bullet.position += bullet.direction * bullet.speed * (float)deltaTime;
                bullet.pic.SetBounds((int)bullet.position.X, (int)bullet.position.Y, 50, 50);
            }
        }

        private void DetectCollision()
        {
            // Check if bullet is outside panel boundaries
            foreach (Bullet bullet in bullets)
            {
                if (bullet.pic.Bounds.X < -100 || bullet.pic.Bounds.X > panelGame.Width || 
                    bullet.pic.Bounds.Y < -100 || bullet.pic.Bounds.Y > panelGame.Height)
                {
                    bulletsToRemove.Add(bullet);
                    m_udp.SendTo("RemoveBullet:" + bullets.IndexOf(bullet), m_udp.m_epFrom);

                    continue;
                }
            }

            // If a target is hit by a bullet, destroys the target
            foreach (Bullet bullet in bullets)
            {
                for (int i = 0; i < targets.Count; i++)
                {
                    if (targets[i].Bounds.IntersectsWith(bullet.pic.Bounds))
                    {
                        targetsToRemove.Add(targets[i]);
                        m_udp.SendTo("RemoveTarget:" + i, m_udp.m_epFrom);
                        bulletsToRemove.Add(bullet);
                        m_udp.SendTo("RemoveBullet:" + bullets.IndexOf(bullet), m_udp.m_epFrom);
                    }
                }
            }
        }

        private void RemoveDestroyedObjects()
        {
            // Remove destroyed bullets
            foreach (Bullet bullet in bulletsToRemove)
            {
                panelGame.Controls.Remove(bullet.pic);
                bullets.Remove(bullet);
            }
            bulletsToRemove.Clear();

            // Remove destroyed targets
            foreach (PictureBox target in targetsToRemove)
            {
                panelGame.Controls.Remove(target);
                targets.Remove(target);
                Log("Target remain" + targets.Count);
            }
            targetsToRemove.Clear();
        }

        public void UpdateList()
        {
            Messages message = m_udp.GetNextMessage();
            if (message != null)
            {
                Log("Got Message: " + message.Message);
                var act = message.Message.Split(':');
                switch (act[0])
                {
                    case "Connected":
                        {
                            Log("Client: " + message.RemoteEP);
                            m_udp.isConnected = true;
                            panelGame.Visible = true;
                            AddCharacter();
                            AddTargets();
                            m_udp.SendTo("AddCharacter:" + character.Bounds.X + "," + character.Bounds.Y, message.RemoteEP);
                            m_udp.SendTo("AddTargets:" + targets[0].Bounds.X + "," + targets[0].Bounds.Y + "," +
                                                         targets[1].Bounds.X + "," + targets[1].Bounds.Y + "," +
                                                         targets[2].Bounds.X + "," + targets[2].Bounds.Y, message.RemoteEP);
                            break;
                        }
                    case "Up":
                        {
                            Rectangle b = character.Bounds;
                            character.SetBounds(b.X, b.Y - 5, 100, 100);
                            m_udp.SendTo("MoveCharacter:" + character.Bounds.X + "," + character.Bounds.Y, message.RemoteEP);
                            break;
                        }
                    case "Down":
                        {
                            Rectangle b = character.Bounds;
                            character.SetBounds(b.X, b.Y + 5, 100, 100);
                            m_udp.SendTo("MoveCharacter:" + character.Bounds.X + "," + character.Bounds.Y, message.RemoteEP);
                            break;
                        }
                    case "Left":
                        {
                            Rectangle b = character.Bounds;
                            character.SetBounds(b.X - 5, b.Y, 100, 100);
                            m_udp.SendTo("MoveCharacter:" + character.Bounds.X + "," + character.Bounds.Y, message.RemoteEP);
                            break;
                        }
                    case "Right":
                        {
                            Rectangle b = character.Bounds;
                            character.SetBounds(b.X + 5, b.Y, 100, 100);
                            m_udp.SendTo("MoveCharacter:" + character.Bounds.X + "," + character.Bounds.Y, message.RemoteEP);
                            break;
                        }
                    case "ShootTo":
                        {
                            string[] pos = act[1].Split(',');
                            Point mouse = new Point(int.Parse(pos[0]), int.Parse(pos[1]));
                            Shoot(mouse);
                            break;
                        }
                }
            }

            if (targets.Count >= 0)
            {
                UpdateGame();
            }
        }

        private void ResetServer()
        {
            // Reset UI
            panelGame.Controls.Clear();
            panelGame.Visible = false;

            // Reset game state variables
            m_udp.isConnected = false;
            targets.Clear();
            bullets.Clear();
            targetsToRemove.Clear();
            bulletsToRemove.Clear();
        }

        private void Log(object item)
        {
            listBoxServer.Items.Add(item);
        }
    }
}
