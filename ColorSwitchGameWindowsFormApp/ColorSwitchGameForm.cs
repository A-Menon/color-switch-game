using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace ColorSwitchGameWindowsFormApp
{
    public partial class ColorSwitchGameForm : Form
    {
        public ColorSwitchGameForm()
        {
            InitializeComponent();
        }

        //box arrays+lists
        PictureBox[] boxes = new PictureBox[4]; //box array
        List<Color> boxColors = new List<Color>(); //list of possible box colors
        List<Int32> indices = new List<Int32>(); //list of box indices to id them

        //game operation
        bool running = false; //game activity
        int speed = 2;
        int score = 0;
        int level = 1;
        int tick = 0; //tick for timer

        //obstacle appearance and intervals
        int nextBox = 0; //index of next box
        int lastBox = -1; //index of previous box
        int interval = 225; //distance between box appearances
        int intMin = 150; //min range value for box interval
        int intMax = 300; //max range value for box interval
        int intervalCounter = 300; //checks time since last box appeared
        int currentIndex = 0; //current box index
        bool[] active = new bool[4] { false, false, false, false }; //box visibility + movement

        //color generation
        bool containsColor = false; //already in list?

        private string HIGH_SCORE_FULL_PATH; //path for high score file
        string[] initialList = new string[10]; //initials of top 10 players
        Int32[] scores = new Int32[10]; //top 10 scores
        string initials = String.Empty; //player initials

        private void ColorSwitchGameForm_Load(object sender, EventArgs e)
        {
            //assign boxes to box array
            boxes[0] = pictureBox2;
            boxes[1] = pictureBox3;
            boxes[2] = pictureBox4;
            boxes[3] = pictureBox5;

            //adds in starting box colors to array
            boxColors.Add(ColorTranslator.FromHtml("#34568B"));
            boxColors.Add(ColorTranslator.FromHtml("#FF6F61"));
            boxColors.Add(ColorTranslator.FromHtml("#6B5B95"));
            boxColors.Add(ColorTranslator.FromHtml("#88B04B"));

            //adds in box indices for differentiation
            indices.Add(0);
            indices.Add(1);
            indices.Add(2);
            indices.Add(3);

            // initialize the path to the high scores file 
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            appDataPath = Path.Combine(appDataPath, "ColorSwitchGame");
            Directory.CreateDirectory(appDataPath);
            HIGH_SCORE_FULL_PATH = Path.Combine(appDataPath, "HighScores.txt");
            updateScoreBoard();
        }

        //creates or updates scores
        private void updateScoreBoard()
        {
            //checks if game has been played before on this account
            if (File.Exists(HIGH_SCORE_FULL_PATH))
            {
                textBox1.Text = String.Empty;
                using (StreamReader sr = new StreamReader(HIGH_SCORE_FULL_PATH))
                {
                    string line = sr.ReadLine();
                    int counter = 0;
                    while (line != null && counter < 10)
                    {
                        // parse the line as comma separated values 
                        string[] parts = line.Split(',');
                        initialList[counter] = parts[0];
                        scores[counter] = int.Parse(parts[1]);

                        // append the formatted high score to the textBox 
                        textBox1.AppendText($"{initialList[counter]} {String.Format("{0:00}", scores[counter])}\r\n");
                        counter++;
                        line = sr.ReadLine();
                    }

                }
            }
            else
            {
                //creates new high score file for first time players
                File.Create(HIGH_SCORE_FULL_PATH);
            }
        }

        //game initialization
        private void runGame()
        {
            timer1.Start();
            shuffleColors();
            for (int i = 0; i < 4; i++)
            {
                boxes[i].Top = -boxes[i].Height;
                boxes[i].Visible = true;
            }
            boxes[0].BackColor = boxColors[indices[0]];
            pictureBox1.Visible = true;
            pictureBox1.BackColor = boxColors[currentIndex];
            panel1.Focus();
        }

        //changes around color list so that next color is randomized
        private void shuffleColors()
        {
            for (int i = 0; i < indices.Count; i++)
            {
                Random rnd = new Random();
                int temp = indices[i];
                int switchIndex = rnd.Next(indices.Count);
                indices[i] = indices[switchIndex];
                indices[switchIndex] = temp;
            }
        }

        //exit button
        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        //controls downward obstacle movement
        private void moveBoxes()
        {
            intervalCounter += speed;
            for (int i = 0; i < 4; i++)
            {
                if (active[i] == true)
                {
                    //moves active boxes and resets any that hit the bottom
                    boxes[i].Top += speed;
                    if (boxes[i].Top > 445)
                    {
                        active[i] = false;
                        boxes[i].Top = -boxes[i].Height;
                    }
                }
                //collision check
                if ((pictureBox1.Top < (boxes[i].Top + pictureBox1.Height)) && (pictureBox1.Top > (boxes[i].Top - pictureBox1.Height)))
                {
                    //color mismatch
                    if (pictureBox1.BackColor != boxes[i].BackColor)
                    {
                        timer1.Stop();
                        roundEnd();
                    } else
                    {
                        //score increase
                        if (lastBox != i)
                        {
                            score++;

                            //increase difficulty
                            if (score % 10 == 0)
                            {
                                //increases speed and adjusts distance range between boxes to compensate
                                Random rand = new Random();
                                level++;
                                intMin += 50;
                                intMax += 100; //adds to picture box distance to make up for increase in speed
                                indices.Add(indices.Count);
                                
                                //adds new random color
                                while (!containsColor)
                                {
                                    Color newColor = ColorTranslator.FromHtml(String.Format("#{0:X6}", rand.Next(0x1000000))); //adds up to infinite colors
                                    if (!boxColors.Contains(newColor))
                                    {
                                        containsColor = true;
                                        boxColors.Add(newColor);
                                    }
                                }
                                containsColor = false;
                            }
                            lastBox = i;
                        }
                    }
                }
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (running)
            {
                //speed increases with level
                speed = level + 1;

                tick++;

                //randomize distance between consecutive boxes
                Random rand = new Random();
                interval = rand.Next(intMin,intMax);

                //add new box if enough time has elapsed
                if (intervalCounter > interval && active[nextBox] == false)
                {
                    //activate box
                    active[nextBox] = true;

                    //set random color
                    shuffleColors();
                    boxes[nextBox].BackColor = boxColors[indices[rand.Next(indices.Count)]];
                    
                    //reset next box in line and time counter
                    intervalCounter = 0;
                    nextBox = (nextBox + 1) % 4;
                }
                //update score and level
                scoreLabel.Text = "Your Score: " + score;
                levelLabel.Text = "Your Level: " + level;
                moveBoxes();
            }
        }

        //changes color if game active
        private void Space_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (running && e.KeyChar == ' ')
            {
                currentIndex = (currentIndex + 1) % indices.Count;
                pictureBox1.BackColor = boxColors[currentIndex];
            }
        }

        private void endGame()
        {
            //resets variables
            button2.Text = "Restart";
            tick = 0;
            running = false;
            speed = 2;
            nextBox = 0;
            lastBox = -1;
            interval = 225;
            intMin = 150;
            intMax = 300;
            intervalCounter = 300;
            currentIndex = 0;
            level = 1;

            //removes, resets, and hides all boxes
            for (int i = 0; i < 4; i++)
            {
                active[i] = false;
            }
            boxColors.RemoveRange(4, boxColors.Count - 4);
            indices.Clear();
            indices.Add(0);
            indices.Add(1);
            indices.Add(2);
            indices.Add(3);
        }

        //start, stop, and restart button
        private void button2_Click(object sender, EventArgs e)
        {
            if (!running)
            {
                button2.Text = "Stop";
                running = true;
                nextBox = 0;
                runGame();
            } else
            {
                score = 0;
                endGame();
            }
        }

        //user id request form - used if new high score achieved
        public static string GetInitials(string title, string text)
        {
            Form prompt = new Form()
            {
                Width = 300,
                Height = 140,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = title,
                StartPosition = FormStartPosition.CenterScreen
            };

            Label textLabel = new Label() { Left = 10, Top = 10, Text = text, Width = 250 };
            TextBox textBox = new TextBox() { Left = 10, Top = 30, Width = 250 };
            Button okButton = new Button()
            {
                Text = "OK",
                Left = 10,
                Width = 50,
                Top = 60,
                DialogResult = DialogResult.OK
            };

            okButton.Click += (sender, e) => { prompt.Close(); };
            prompt.Controls.Add(textBox);
            prompt.Controls.Add(okButton);
            prompt.Controls.Add(textLabel);
            prompt.MaximizeBox = false;
            prompt.MinimizeBox = false;
            prompt.AcceptButton = okButton;
            prompt.ShowDialog();
            return textBox.Text.Trim();
        }
        
        //check if score is a new High Score and update top score list if it is
        private bool newHS()
        {
            string title = "New High Score!";
            bool newHS;
            bool validInput;
            newHS = (score > scores[9]) || (score == 0 && initialList[9] == null);

            if (newHS)
            {
                //sentinel loop for valid user id "initials"
                do
                {
                    validInput = true;
                    initials = GetInitials(title, "Enter your initials:");
                    if (initials.Length != 3)
                    {
                        validInput = false;
                        title = "Initials must have 3 letters!";
                    }
                    foreach (char letter in initials)
                    {
                        if (!Char.IsLetter(letter))
                        {
                            validInput = false;
                            title = "All initials must be letters!";
                        }
                    }
                } while (!validInput);
                
                //used for high score placement
                int index = -1;

                //add new high score and shift previous scores accordingly
                for (int i = 0; i < 10; i++)
                {
                    if ((score > scores[i]) || (score == 0 && initialList[i] == null))
                    {
                        index = i;
                        for (int j = 9; j > Math.Max(index - 1,0); j--)
                        {
                            if (initialList[j - 1] != null)
                            {
                                scores[j] = scores[j - 1];
                                initialList[j] = initialList[j - 1];
                            }
                        }
                        break;
                    }
                }
                scores[index] = score;
                initialList[index] = initials;
            }

            return newHS;
        }

        //game over events
        private void roundEnd()
        {
            endGame();

            //update high score document if needed
            if (newHS())
            {
                File.WriteAllText(HIGH_SCORE_FULL_PATH, String.Empty);
                using (StreamWriter sr = new StreamWriter(HIGH_SCORE_FULL_PATH))
                {
                    for (int i = 0; i < 10; i++)
                    {
                        if (initialList[i] != null)
                        {
                            sr.WriteLine(initialList[i] + "," + scores[i]);
                        }
                    }
                }
            }

            //reset score and update client-side scoreboard if necessary
            updateScoreBoard();
            score = 0;
        }
    }
}
