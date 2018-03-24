﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using WordCompletion;

namespace WordCompletedNotes
{
    public partial class MainForm : Form
    {
        IComplementarable dictionary;

        ViewFitter view;

        AutocompletionForm autoForm;

        bool newStart;

        string openFile = "";

        public MainForm()
        {
            InitializeComponent();

            dictionary = new SimpleCompletion();

            autoForm = new AutocompletionForm(this);

            view = new ViewFitter(autoForm, textBox);
        }

        private void textBox_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Down:
                case Keys.Up:
                    e.IsInputKey = true;
                    break;
            }
        }

        private void textBox_KeyDown(object sender, KeyEventArgs e)
        {
            newStart = char.IsControl((char)e.KeyCode) || ((int)e.KeyCode < 41 && (int)e.KeyCode > 36);

            switch (e.KeyCode)
            {
                case Keys.Down:
                    if (autoForm.Visible)
                    {
                        autoForm.TrySelectNextItem();
                        e.Handled = true;
                    }
                    break;
                case Keys.Up:
                    if (autoForm.Visible)
                    {
                        autoForm.TrySelectPreviousItem();
                        e.Handled = true;
                    }
                    break;
                default:
                    autoForm.Hide();
                    break;
            }
        }

        private void textBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            string fromBeginToSelection = textBox.Text.Substring(0, textBox.SelectionStart);
            string lastWord = Regex.Match(fromBeginToSelection, @"\w+\Z").Value.ToLower();

            view.UpdateListBoxPosition();
            if (char.IsWhiteSpace(e.KeyChar) || char.IsPunctuation(e.KeyChar) || e.KeyChar == '\n')
            {
                newStart = true;
                dictionary.Insert(lastWord);
            }
            else
            {
                string nextWord = lastWord + e.KeyChar;

                autoForm.ClearAndHide();

                if (nextWord != "")
                {
                    List<string> list = dictionary.FindMostUsedMatches(nextWord.ToLower());
                    if (list.Any())
                    {
                        view.AdjustAndShowPrompts(list);
                    }
                }
            }
        }

        private void textBox_KeyUp(object sender, KeyEventArgs e)
        {
            newStart = true;
        }

        public void ChangeEditedWord()
        {
            string fromBeginToSelection = textBox.Text.Substring(0, textBox.SelectionStart);
            Match lastWordMatch = Regex.Match(fromBeginToSelection, @"\w+\Z");
            string lastWord = lastWordMatch.Value;
            textBox.Text = textBox.Text.Remove(lastWordMatch.Index, textBox.SelectionStart - lastWordMatch.Index);
            string wordToInsert = autoForm.ListBox.SelectedItem.ToString();
            for (int i = 0; i < lastWord.Length; i++)
            {
                if (char.IsUpper(lastWord[i]))
                {
                    wordToInsert = UpperStringAt(wordToInsert, i);
                }
            }
            textBox.Text = textBox.Text.Insert(lastWordMatch.Index, wordToInsert);
            textBox.SelectionStart = lastWordMatch.Index + wordToInsert.Length;
        }

        private string UpperStringAt(string input, int index)
        {
            char[] array = input.ToCharArray();
            array[index] = char.ToUpper(array[index]);
            return new string(array);
        }

        private void MainForm_MoveOrResize(object sender, EventArgs e)
        {
            view.UpdateListBoxPosition();
        }

        private void textBox_Click(object sender, EventArgs e)
        {
            autoForm.Hide();
            view.UpdateListBoxPosition();
        }

        private void exitMenu_Click(object sender, EventArgs e)
        {
            //Application.Exit();
            Close();
        }

        private void saveAsMenu_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();

            saveFileDialog.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            saveFileDialog.FilterIndex = 1;
            saveFileDialog.RestoreDirectory = true;

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                string path = saveFileDialog.FileName;
                File.WriteAllText(path, textBox.Text);
                openFile = path;
                saveMenu.Enabled = true;
            }
        }

        private void openMenu_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            openFileDialog.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            openFileDialog.FilterIndex = 1;
            openFileDialog.RestoreDirectory = true;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string path = openFileDialog.FileName;
                if (File.Exists(path) == true)
                {
                    textBox.Text = File.ReadAllText(openFileDialog.FileName);
                    openFile = path;
                    saveMenu.Enabled = true;
                }
            }
        }

        private void saveMenu_Click(object sender, EventArgs e)
        {
            if (openFile != "")
            {
                if (File.Exists(openFile) == true)
                {
                    File.WriteAllText(openFile, textBox.Text);
                }
            }
        }

        private void newMenu_Click(object sender, EventArgs e)
        {
            textBox.Clear();
            openFile = "";
            saveMenu.Enabled = false;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            Dictionary<string, int> init = new Dictionary<string, int>();
            init.Add("alinka", 2); init.Add("wesoła", 1); init.Add("dziewczynka", 1); init.Add("ale", 1); init.Add("ma", 3); init.Add("astmę", 1);

            OutputTimings(new SimpleCompletion(init));
            OutputTimings(new TrieCompletion(init));
        }

        private void SaveWordsToTextfile(IComplementarable dictionary)
        {
            string path = Application.StartupPath + @"\Words.txt";

            using (FileStream fs = File.Create(path))
            {
                Dictionary<string, int> words = dictionary.GetAllWords();
                for (int i = 0; i < words.Count; i++)
                {
                    Byte[] line = new UTF8Encoding(true).GetBytes(words.Keys.ElementAt(i) + ";" + words.Values.ElementAt(i) + "\n");
                    if (i == words.Count - 1)
                    {
                        byte[] tmp = new byte[line.Length - 1];
                        Array.Copy(line, tmp, line.Length - 1);
                        line = tmp;
                    }
                    fs.Write(line, 0, line.Length);
                }
            }
        }

        private void SaveWordsToDatabase(IComplementarable dictionary)
        {
            string connetionString = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=" + Application.StartupPath + @"\WordsDatabase.mdf;Integrated Security=True";
            SqlConnection cnn = new SqlConnection(connetionString);
            cnn.Open();

            SqlCommand cmdDelete = new SqlCommand("DELETE FROM Words", cnn);
            cmdDelete.ExecuteNonQuery();

            Dictionary<string, int> words = dictionary.GetAllWords();
            for (int i = 0; i < words.Count; i++)
            {
                SqlCommand cmdInsert = new SqlCommand("INSERT INTO Words (Word,UsesCount) VALUES ('" + words.Keys.ElementAt(i) + "','" + words.Values.ElementAt(i) + "')", cnn);
                cmdInsert.ExecuteNonQuery();
            }

            cnn.Close();
        }

        private void ReadWordsFromTextfile()
        {
            string path = Application.StartupPath + @"\Words.txt";
            if (File.Exists(path) == true)
            {
                List<string> lines = File.ReadLines(path).ToList();
                foreach (string line in lines)
                {
                    string[] columns = line.Split(';');
                    string word = columns[0];
                    int count = Int32.Parse(columns[1]);
                    Console.WriteLine(word + ", " + count);
                }

            }
        }

        private void ReadWordsFromDatabase()
        {
            string connetionString = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=" + Application.StartupPath + @"\WordsDatabase.mdf;Integrated Security=True";
            SqlConnection cnn = new SqlConnection(connetionString);
            cnn.Open();

            SqlCommand cmdSelect = new SqlCommand("SELECT * FROM Words", cnn);
            SqlDataReader dataReader = cmdSelect.ExecuteReader();

            while (dataReader.Read())
            {
                string word = dataReader["Word"].ToString();
                int count = Int32.Parse(dataReader["UsesCount"].ToString());
                Console.WriteLine(word + ", " + count);
            }
            cnn.Close();
        }

        private void OutputReadTime(Action method)
        {
            Console.WriteLine("------" + method.Method + "------");
            var watch = System.Diagnostics.Stopwatch.StartNew();
            method();
            var elapsedMs = watch.ElapsedTicks;
            Console.WriteLine(elapsedMs + " ticks");
            Console.WriteLine("------\n");
        }

        private void OutputWriteTime(Action<IComplementarable> method, IComplementarable dictionary)
        {
            Console.WriteLine("------" + method.Method + "------");
            var watch = System.Diagnostics.Stopwatch.StartNew();
            method(dictionary);
            var elapsedMs = watch.ElapsedTicks;
            Console.WriteLine(elapsedMs + " ticks");
            Console.WriteLine("------\n");
        }

        private void OutputCompletions(IComplementarable dictionary, string match)
        {
            List<string> completions = dictionary.FindMostUsedMatches(match);

            Console.WriteLine("\tFinding '" + match + "' completions...");
            foreach (string c in completions)
            {
                Console.WriteLine("\t-" + c);
            }
            Console.WriteLine("\t...done.");
        }

        private void OutputTimings(IComplementarable dictionary)
        {
            Console.WriteLine("\t ~~~~~ " + dictionary.GetType() + " ~~~~~ \n");
            OutputWriteTime(SaveWordsToTextfile, dictionary);
            OutputWriteTime(SaveWordsToDatabase, dictionary);
            OutputReadTime(ReadWordsFromTextfile);
            OutputReadTime(ReadWordsFromDatabase);
            Console.WriteLine();
        }
    }
}
