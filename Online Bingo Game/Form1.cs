using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Threading;

namespace Online_Bingo_Game
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            inilializeBoard();
        }
        int port;
        int winOpt; //determines the win method
        Thread connection;
        TcpClient client = new TcpClient(); //The client object
        Button[,] board = new Button[3,3];
        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            //MessageBox.Show(e.X.ToString() + "," + e.Y.ToString());
        }
        private void inilializeBoard() 
        {
            int x = 2, y = 359;
            int side = 282 / 3;
            for (int row = 0; row < 3; row++)
                for (int column = 0; column < 3; column++)
                {
                    Button cell = new Button();
                    cell.Text = "";
                    cell.Font = new Font("Arial", 12, GraphicsUnit.Pixel);
                    cell.BackColor = Color.LightSkyBlue;
                    cell.Location = new Point(x + side * column, y + side * row);
                    cell.Size = new Size(side, side);
                    Controls.Add(cell);
                    //cell.Click += doTurn; //Attaching the event
                    board[row, column] = cell;
                }
        }
        private void connect()
        {
            port = int.Parse(textBox1.Text);
            try
            {
                client.Connect("127.0.0.1", port); //Connects to the remote server

                connection = new Thread(receiveData);
                connection.Start();
                button1.Enabled = false;
                sendData(textBox2.Text);
                richTextBox1.AppendText("Connected Successfully!");
                richTextBox1.AppendText(Environment.NewLine);
            }
            catch
            {
                richTextBox1.AppendText("Unable to connect. Try again.");
                richTextBox1.AppendText(Environment.NewLine);
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            connect();
        }
        private void receiveData() 
        {
            
                string data = "";
                while (true)
                {
                    try
                    {
                        byte[] stream = new byte[100025];
                        NetworkStream clientS = client.GetStream();
                        clientS.Read(stream, 0, client.ReceiveBufferSize);
                        data = Encoding.ASCII.GetString(stream);
                        data = data.Substring(0, data.IndexOf("$"));
                        handleData(data);
                    }
                    catch 
                    {
                        if (this.InvokeRequired)
                        {
                            Invoke(new Action(() =>
                            {
                                button1.Enabled = true;
                            }));
                        }
                        else
                        {
                            button1.Enabled = true;
                            connection.Abort();
                        }
                    }
                }
            
        }
        private void handleData(string newData)
        {
            string type = newData.Substring(0, newData.IndexOf(":")); //Gets the type of the message (which is declared at the start until the ':'
            switch (type)
            {
                case "WINOPT":
                    winOpt = int.Parse(newData.Remove(0,newData.IndexOf(":") + 1));
                    break;
                case "WORDLIST":
                    if (this.InvokeRequired)
                    {
                        Invoke(new Action(() =>
                        {
                            synchronizeBoards(generateBoard(newData.Remove(0, newData.IndexOf(":") + 1)));
                        }));
                    }
                    else
                    {
                        synchronizeBoards(generateBoard(newData.Remove(0, newData.IndexOf(":") + 1)));
                    }
                    break;
                case "WORD":
                    if (this.InvokeRequired)
                    {
                        Invoke(new Action(() =>
                        {
                            wordHandle(newData.Remove(0, newData.IndexOf(":") + 1));
                        }));
                    }
                    else
                    {
                        wordHandle(newData.Remove(0, newData.IndexOf(":") + 1));
                    }
                    break;
                case "MESSAGE":
                    printData(newData.Remove(0,newData.IndexOf(":") + 1));
                    break;
                default: 
                    break;
            }
        }
        private string[,] generateBoard(string list) 
        {
            string[,] temp = new string[3, 3];
            string part = list;
            for(int row = 0; row < temp.GetLength(0);row++)
                for (int item = 0; item < temp.GetLength(1); item++)
                {
                    part = list.Substring(0, list.IndexOf("|")); //Gets the next word in the list
                    list = list.Remove(0, list.IndexOf("|") + 1); //Removes the current word from the list
                    temp[row, item] = part;
                }
            return temp;
        }
        private void synchronizeBoards(string[,] sBoard)
        { 
            for(int row = 0; row < board.GetLength(0);row++)
                for (int item = 0; item < board.GetLength(1); item++)
                    board[row, item].Text = sBoard[row, item];
        }
        private void sendData(string data) 
        { 
            try
            {
                NetworkStream clientS = client.GetStream();
                byte[] stream;
                stream = Encoding.ASCII.GetBytes(data + "$");
                clientS.Write(stream, 0, stream.Length);
                clientS.Flush();
            }
            catch
            {
                connection.Abort();
                client.Close();
                richTextBox1.AppendText("You were disconnected.");
                richTextBox1.AppendText(Environment.NewLine);
            }
        }
        private void printData(string data)
        {
            if (this.InvokeRequired)
            {
                Invoke(new Action(() =>
                {
                    richTextBox1.AppendText(data);
                    richTextBox1.AppendText(Environment.NewLine);
                }));
            }
            else
            {
                richTextBox1.AppendText(data);
                richTextBox1.AppendText(Environment.NewLine);
            }
        }
        private void wordHandle(string word)
        {
            nextWord.Text = word; //Updates the word
            for (int i = 0; i < board.GetLength(0); i++)
                for (int j = 0; j < board.GetLength(1); j++)
                    if (board[i, j].Text.Equals(word))
                    {
                        board[i, j].Enabled = false;
                        board[i, j].BackColor = Color.Wheat;
                    }
            //Check for winner
            if (winOpt == 1)
            {
                if (!board[0, 0].Enabled && !board[0, 1].Enabled && !board[0, 2].Enabled)
                    sendData("BINGO");
                else if (!board[1, 0].Enabled && !board[1, 1].Enabled && !board[1, 2].Enabled)
                    sendData("BINGO");
                else if (!board[2, 0].Enabled && !board[2, 1].Enabled && !board[2, 2].Enabled)
                    sendData("BINGO");
                else if (!board[0, 0].Enabled && !board[1, 0].Enabled && !board[2, 0].Enabled)
                    sendData("BINGO");
                else if (!board[0, 1].Enabled && !board[1, 1].Enabled && !board[2, 1].Enabled)
                    sendData("BINGO");
                else if (!board[0, 2].Enabled && !board[1, 2].Enabled && !board[2, 2].Enabled)
                    sendData("BINGO");
                else if (!board[0, 0].Enabled && !board[1, 1].Enabled && !board[2, 2].Enabled)
                    sendData("BINGO");
                else if (!board[2, 0].Enabled && !board[1, 1].Enabled && !board[0, 2].Enabled)
                    sendData("BINGO");
            }
            else 
            { 
                bool found = false;
                for (int row = 0; row < board.GetLength(0); row++)
                    for (int item = 0; item < board.GetLength(1); item++)
                        if (board[row, item].Enabled)
                            found = true;
                if (!found)
                    sendData("BINGO");
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            client.Close();
        }
    }
}
