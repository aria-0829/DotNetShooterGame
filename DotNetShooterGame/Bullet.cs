using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DotNetShooterGame
{
    internal class Bullet
    {
        public PictureBox pic = new PictureBox();
        public Vector2 position = new Vector2(0, 0);
        public Vector2 direction = new Vector2(0, 0);
        public int speed = 100;
    }
}
