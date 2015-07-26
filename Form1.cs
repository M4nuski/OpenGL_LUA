using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using NLua.Exceptions;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Platform;

using NLua;

namespace LUA
{
    public partial class Form1 : Form
    {
        public IWindowInfo WindowInfo;
        public GraphicsContext WindowContext;

        public Lua state = new Lua();
        public string script;
        public int cnt;
        public double x, y;

        public Stopwatch sw = new Stopwatch();

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Closing += Form1_Closing;

            WindowInfo = Utilities.CreateWindowsWindowInfo(panel1.Handle);
            var WindowMode = new GraphicsMode(32, 24, 0, 0, 0, 2);
            WindowContext = new GraphicsContext(WindowMode, WindowInfo, 2, 0, GraphicsContextFlags.Debug);

            WindowContext.MakeCurrent(WindowInfo);
            WindowContext.LoadAll(); // as IGraphicsContextInternal)

            WindowContext.SwapInterval = 1;
            GL.Viewport(0, 0, panel1.Width, panel1.Height);

            GL.Enable(EnableCap.DepthTest);
            GL.Disable(EnableCap.CullFace);
            GL.ClearColor(Color.DarkBlue);


            try
            {
                state.LoadCLRPackage();
                state.DoString(@" import ('LUA', 'LUA') ");
            }
            catch (LuaException ex)
            {
                MessageBox.Show(ex.Message, "LUA Package Exception", MessageBoxButtons.OK);
            }

            state["x"] = 0.0;
            state["y"] = 1.0;
            state["fn"] = 0;
            script = textBox1.Text;

            timer1.Enabled = true;
        }

        public void Form1_Closing(object sender, EventArgs e)
        {
            //
        }

        private void button1_Click(object sender, EventArgs e)
        {
            script = textBox1.Text;
            timer1.Enabled = true;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            sw.Start();
            
            WindowContext.MakeCurrent(WindowInfo);
            //    GL.Viewport(0, 0, panel1.Width, panel1.Height);
            GL.Clear(ClearBufferMask.ColorBufferBit |
                     ClearBufferMask.DepthBufferBit);

            cnt++;
            state["fn"] = cnt;
            label1.Text = string.Format("fn={0} x={1} y={2}", cnt, x, y);

            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.Frustum(-3.0d, 3.0d, -3.0d, 3.0d, 1.0d, 10.0d);

            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();

            try
            {
                state.DoString(script);
            }
            catch (LuaException ex)
            {
                timer1.Enabled = false;
                MessageBox.Show(ex.Message, "LUA Parser Exception", MessageBoxButtons.OK);
            }

            x = state.GetNumber("x");
            y = state.GetNumber("y");
            var count = state.GetNumber("count");

            GL.Translate(0.0d, 0.0d, -5.0d);
            GL.Scale(3, 3, 3);
            GL.Rotate(x, 1.0f, 0.0f, 0.0f);
            GL.Rotate(y, 0.0f, 1.0f, 0.0f);

            GL.Begin(PrimitiveType.Triangles);
            GL.Color4(1.0f, 0.0f, 0.0f, 1.0f);
            GL.Vertex3(0.0f, 1.0f, 0.0f);

            GL.Color4(0.0f, 1.0f, 0.0f, 1.0f);
            GL.Vertex3(-1.0f, 0.0f, 1.0f);

            GL.Color4(0.0f, 0.0f, 1.0f, 1.0f);
            GL.Vertex3(1.0f, 0.0f, -1.0f);
            GL.End();

            try
            {
                var opt = state.DoString("return GetPoints()")[0] as LuaTable;
                if (opt != null)
                {
                    for (var i = 0; i < count; i++)
                    {
                        var pt = (LuaTable)opt[i];
                        if (pt != null)
                        {
                            GL.Begin(PrimitiveType.Points);
                            GL.Color3((double) pt["R"], (double) pt["G"], (double) pt["B"]);
                            GL.Vertex3((double) pt["X"], (double) pt["Y"], (double) pt["Z"]);
                            GL.End();
                        }
                    }
                }
            }
            catch (LuaException ex)
            {
                timer1.Enabled = false;
                MessageBox.Show(ex.Message, "LUA Parser Exception", MessageBoxButtons.OK);
            }
            
            sw.Stop();
            Text = (100 * sw.ElapsedMilliseconds / timer1.Interval).ToString("d2") + "%"; 
            sw.Reset();
            WindowContext.SwapBuffers();
        }

        private void panel1_Resize(object sender, EventArgs e)
        {
            WindowContext.MakeCurrent(WindowInfo);
            GL.Viewport(0, 0, panel1.Width, panel1.Height);
        }

    }
}
