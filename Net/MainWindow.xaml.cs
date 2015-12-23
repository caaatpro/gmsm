using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Forms;
using CheckBox = System.Windows.Controls.CheckBox;
using MessageBox = System.Windows.MessageBox;

namespace Net
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        // Содержимое файла
        public List<string> FileLines = new List<string> ();
        public string FileText = "";

        // Каталог для сохранения файлов
        public string SaveFolder = "";

        // Форматы файлов
        public string FileFilter = "CSV (*.csv)|*.csv|Текстовый документ (*.txt)|*.txt|Все файлы (*.*)|*.*";

        // Узлы
        public List<string> Nodes = new List<string>();

        // Элементы в узле
        public List<List<string>> NodesElems = new List<List<string>>();

        // Элементы
        public List<string> Elems = new List<string>();

        // Библиотека net файла
        public int Dll = -1; // -1 = не определена, 0 = orCalay90, 1 = orPcad, 2 = Allegro

        // Матрицы
        public List<List<string>> R = new List<List<string>>();
        public List<List<string>> Q = new List<List<string>>();
        
	    public int QFile;
	    public int RFile;


        // Диалог открытия
        private void ButtonOpen_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Filter = @"|*.net"};

            if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
#if DEBUG
            Console.WriteLine(1);
            Console.WriteLine(dlg.FileName);
#endif
            var fileInfo = new FileInfo(dlg.FileName);

            // проверка формата
            if (fileInfo.Extension.ToLower() != ".net") return;
        
            // Убираем старые данные
            Reset();


            // Блокируем кнопки и поля
            Button.IsEnabled = false;

            FileInput.Text = fileInfo.FullName;

            Progress.Minimum = 0;
            Progress.Maximum = 3;

            ProgressLabel.Content = @"Чтение файла";
            Progress.Value = 1;

            using (var sr = fileInfo.OpenText())
            {
                var temp = "";
                while (true)
                {
                    var str = sr.ReadLine();
                    // Если достигнут конец файла, прерываем считывание.
                    if (str == null) break;

                    // Читаем строку из файла во временную переменную.
                    temp += str;
                    
                    // обработка - убираем лишние пробылы
                    temp = Regex.Replace(temp, " +", " ");

                    if (temp.Length == 0) continue;
                    if (temp.Substring(temp.Length - 1, 1) == ",") continue;
                    temp = Regex.Replace(temp, ",", " ");

                    // Пишем считанную строку в итоговую переменную.
                    FileLines.Add(temp);
                    FileText += temp;
                    temp = "";
                }
            }
        
            ProgressLabel.Content = @"Определение библиотеки";
            Progress.Value = 2;

            // Опредееление библиотеки net файла
            //orPcad
            if (FileText.Contains(@"{COMPONENT"))
            {
                Dll = 1;
                DllNameLabel.Content = @"orPcad.dll";
            }
            // Allegro
            else if (FileText.Contains(@"$PACKAGES"))
            {
                Dll = 2;
                DllNameLabel.Content = @"Allegro.dll";
            }
            else // orCalay90
            {
                Dll = 0;
                DllNameLabel.Content = @"orCalay90.dll";
            }

            // чтение в соотвествии с выбранной библиотекой
            // подсчет узлов и элементов
            ReadNetFile();


            if (Elems.Count <= 0 || Nodes.Count <= 0)
            {
                ProgressLabel.Content = @"Ошибка чтения файла";
                Progress.Value = 0;
            }
            else
            {
                // записываем количества узлов и элементов
                CountElems.Content = Elems.Count;
                CountNodes.Content = Nodes.Count;
            }
        }

        // Диалог сохранения матрицы Q
        private void ButtonSave_Q_Click(object sender, RoutedEventArgs e)
        {
	        var saveFileDialog = new SaveFileDialog
	        {
		        Title = @"Матрица Q",
		        Filter = FileFilter,
		        FilterIndex = 1,
		        RestoreDirectory = true
	        };

            if (saveFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
            FileOutputQ.Text = saveFileDialog.FileName;
            QFile = saveFileDialog.FilterIndex;
        }

        // Диалог сохранения матрицы R
        private void ButtonSave_R_Click(object sender, RoutedEventArgs e)
        {
	        var saveFileDialog = new SaveFileDialog
	        {
		        Title = @"Матрица R",
		        Filter = FileFilter,
		        FilterIndex = 1,
                RestoreDirectory = true
            };

            if (saveFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
            FileOutputR.Text = saveFileDialog.FileName;
            RFile = saveFileDialog.FilterIndex;
        }

        // Генерирование
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (Elems.Count == 0 || Nodes.Count == 0)
            {
                Progress.Value = 0;
                ProgressLabel.Content = @"Ошибка! Узлы и/или элементы не найдены.";

                return;
            }

            var sw = new Stopwatch();
            sw.Start();


            // Строим матрицу R
            if (FileOutputR.Text != "")
            {
                ProgressLabel.Content = @"Построение матрицы R";
                Progress.Value = 1;
                CreateR();
            }

            // Строим матрицу Q
            if (FileOutputQ.Text != "")
            {
                ProgressLabel.Content = @"Построение матрицы Q";
                Progress.Value = 3;
                CreateQ();
            }

            // Проверка пути сохранения
            if ((CheckBoxQ.IsChecked == true && FileOutputQ.Text == "") || (CheckBoxR.IsChecked == true && FileOutputR.Text == ""))
            {
                Progress.Value = 0;
                ProgressLabel.Content = @"Ошибка! Пустое имя пути не допускается.";

                return;
            }

            // Сохраняем матрицы в файл
            ProgressLabel.Content = @"Сохранение файлов матриц";
            Progress.Value = 4;

            SaveFiles();
  

            sw.Stop();
            MessageBox.Show((sw.ElapsedMilliseconds).ToString(CultureInfo.InvariantCulture) + " мс");


            ProgressLabel.Content = @"Сохренение завершено";
            Progress.Value = 0;
            
            R = new List<List<string>>();
            Q = new List<List<string>>();
        }

        // Сохранение файлов матриц
        private void SaveFiles()
        {
            // матрица R
            if (CheckBoxR.IsChecked == true)
            {
                switch (RFile)
                {
                    case 1:
                        using (var str = new StreamWriter(FileOutputR.Text, false))
                        {
                            foreach (var t in R)
                            {
                                foreach (var e in t)
                                {
                                    str.Write(e.Trim() + @";");
                                }
                                str.WriteLine();
                            }
                            str.Close();
                        }
                        break;
                    default: // 2 - .txt
                        using (var str = new StreamWriter(FileOutputR.Text, false))
                        {
                            foreach (var t in R)
                            {
                                foreach (var e in t)
                                {
                                    str.Write(e.Trim() + @" ");
                                }
                                str.WriteLine();
                            }
                            str.Close();
                        }
                        break;
                }
            }

            // матрица Q
            if (CheckBoxQ.IsChecked == true)
            {
                switch (QFile)
                {
                    case 1:
                        using (var str = new StreamWriter(FileOutputQ.Text, false))
                        {
                            foreach (var t in Q)
                            {
                                foreach (var e in t)
                                {
                                    str.Write(e.Trim() + @";");
                                }
                                str.WriteLine();
                            }
                            str.Close();
                        }
                        break;
                    default: // 2 - .txt
                        using (var str = new StreamWriter(FileOutputQ.Text, false))
                        {
                            foreach (var t in Q)
                            {
                                foreach (var e in t)
                                {
                                    str.Write(e.Trim() + @" ");
                                }
                                str.WriteLine();
                            }
                            str.Close();
                        }
                        break;
                }
            }
        }

        // Чтение файла
        private void ReadNetFile()
        {

            ProgressLabel.Content = @"Подсчёт элементов и узлов";
            Progress.Value = 4;

            // orCalay90
            if (Dll == 0)
            {
                foreach (var line in FileLines)
                {
                    var temp = line.Split(' ');
                    if (line == "") break;
                    var node = temp[0].Trim(); // узел

#if DEBUG
                    Console.WriteLine(node);
#endif

                    // сохраняем узел
                    Nodes.Add(node);

                    var elems = new List<string>();
                    foreach (var t in temp)
                    {
                        if (t == "" || t == node) continue;
                        var elem = t.Substring(0, t.IndexOf('(')).Trim();
                            
                        if (elems.Contains(elem) == false)
                        {
                            elems.Add(elem);
                        }
#if DEBUG
                        Console.WriteLine(elem);
#endif

                        if (Elems.Contains(elem) == false)
                        {
                            Elems.Add(elem);
                        }
                    }

#if DEBUG
                    Console.WriteLine(elems[0]);
#endif

                    // Добавленяем элементы в список элементов узла
                    NodesElems.Add(elems);
                }
            }
            //orPcad
            if (Dll == 1)
            {
                const string paterrn = @"{I[^}]*}";
                for (var m = Regex.Match(FileText, paterrn); m.Success; m = m.NextMatch())
                {
                    //{I TO205AD.PRT E1{CN 1 C02 2 C13 3 C01}
                    var temp = m.Value.Split(' ');

                    // Элемент
                    var elem = (temp[2].Substring(0, temp[2].IndexOf('{'))).Trim();

#if DEBUG
                    Console.WriteLine(elem);
#endif

                    // Сохраняем элемент
                    Elems.Add(elem);

                    const string paterrn2 = @"{CN[^}]*}";
                    for (var m2 = Regex.Match(m.Value, paterrn2); m2.Success; m2 = m2.NextMatch())
                    {
                        var temp2 = m2.Value.Split(' ');
                        // Узлы
                        for (var i = 2; i < temp2.Length; i+=2)
                        {
                            var node = (temp2[i].IndexOf('}') > 0 ? temp2[i].Substring(0, temp2[i].IndexOf('}')) : temp2[i]).Trim();
#if DEBUG
                            Console.WriteLine(node);
#endif

                            // Сохраняем узел
                            if (Nodes.Contains(node) == false)
                            {
                                Nodes.Add(node);

                                // Заготовка для элементов узла
                                NodesElems.Add(new List<string>());
                            }

                            NodesElems[Nodes.IndexOf(node)].Add(elem);

                        }
                    }
                }
            }
            //Allegro
            if (Dll == 2)
            {
                var start = false;
                foreach (var line in FileLines)
                {
                    if (line == "") break;

                    if (line == "$NETS" || line == "$END")
                    {
                        start = true;
                        continue;
                    }

                    if (start == false) continue;
#if DEBUG
                    Console.WriteLine(line);
#endif
                    var temp = line.Split(' ');
                    var node = temp[0].Substring(0, temp[0].IndexOf(';')).Trim(); // узел

#if DEBUG
                    Console.WriteLine(node);
#endif

                    // сохраняем узел
                    Nodes.Add(node);

                    var elems = new List<string>();
                    var i = 0;
                    foreach (var t in temp)
                    {
                        if (i == 0)
                        {
                            i += 1;
                            continue;
                        }
                        i += 1;
#if DEBUG
                        Console.WriteLine(t);
#endif
                        if (t == "" || t == node) continue;
                        var elem = t.Substring(0, t.IndexOf('.')).Trim();

                        if (elems.Contains(elem) == false)
                        {
                            elems.Add(elem);
                        }
#if DEBUG
                        Console.WriteLine(elem);
#endif

                        if (Elems.Contains(elem) == false)
                        {
                            Elems.Add(elem);
                        }
                    }

#if DEBUG
                    Console.WriteLine(elems[0]);
#endif

                    // Добавленяем элементы в список элементов узла
                    NodesElems.Add(elems);
                }
            }

            // Сортируем элементы
            Elems.Sort();

#if DEBUG
            Console.WriteLine();
            Console.WriteLine();
            var ii = 0;
            foreach (var n in NodesElems)
            {
                Console.Write(Nodes[ii]);
                foreach (var e in n)
                {
                    Console.Write(e);
                }
                Console.WriteLine();
                ii++;
            }
            Console.WriteLine();
#endif

            // Сортируем узлы
            var nodesTemp = Nodes.GetRange(0, Nodes.Count);
            var nodesElemsTemp = NodesElems.GetRange(0, NodesElems.Count);
            Nodes.Sort();
            for (int i = 0; i < Nodes.Count; i++)
            {
#if DEBUG
                // в элементы текущего узла
                Console.WriteLine();
                Console.WriteLine(Nodes[i] + @" " + nodesTemp[i]);
#endif
                NodesElems[i] = nodesElemsTemp[nodesTemp.IndexOf(Nodes[i])];
            }

#if DEBUG
            ii = 0;
            foreach (var n in NodesElems)
            {
                Console.Write(Nodes[ii]);
                foreach (var e in n)
                {
                    Console.Write(e);
                }
                Console.WriteLine();
                ii++;
            }
#endif

            ProgressLabel.Content = @"Чтение файла завершено";
            Progress.Value = 4;
        }


        // матрица R
        private void CreateR()
        {
            // перввая строка
            // пробел и элменты
            // перебираем все элементы
            var line = new List<string> {""};
            line.AddRange(Elems);

            // добавляем первую строку в матрицу R
            R.AddRange(new[] { line });


            // создаём пустую матрицу
            foreach (var t in Elems)
            {
                // очищаем строку и кладём первый элемент
                line = new List<string> { t };
                for (var j = 0; j < Elems.Count; j++)
                {
                    line.Add("0");
                }
         
                R.AddRange(new[] { line });
            }

            // перебираем все элементы
            // заполняем матрицу
            for (var ei = 0; ei < Elems.Count; ei++)
            {
                // преебираем элементы узлов
                for (var i = 0; i < NodesElems.Count; i++)
                {
                    // этот элемент есть в узле
                    if (!NodesElems[i].Contains(Elems[ei])) continue;
#if DEBUG
                    Console.WriteLine(Elems[ei] + @" в " + Nodes[i]);
#endif
                    // преебираем элементы узла и заполняем матрицу
                    for (var j = 0; j < NodesElems[i].Count; j++)
                    {
#if DEBUG
                        Console.WriteLine(Elems[ei] + @" и " + NodesElems[i][j]);
#endif
                        if (Elems[ei] == NodesElems[i][j]) continue;
                        var ei2 = Elems.IndexOf(NodesElems[i][j]) + 1;
                        R[ei + 1][ei2] = (int.Parse(R[ei + 1][ei2]) + 1).ToString();
                    }
                }
            }

#if DEBUG
            Console.WriteLine(@"R");
            foreach (var t in R)
            {
                foreach (var e in t)
                {
                    Console.Write(@"    " + e);
                }
                Console.WriteLine();
            }
#endif
        }

        // Обнуление переменных
        private void Reset()
        {
            FileLines = new List<string>();
            FileText = "";
            Nodes = new List<string>();
            NodesElems = new List<List<string>>();
            Elems = new List<string>();

            CheckBoxR.IsChecked = false;
            CheckBoxQ.IsChecked = false;

            FileOutputR.Text = "";
            FileOutputQ.Text = "";
            FileOutputR.IsEnabled = false;
            FileOutputQ.IsEnabled = false;
            
            QFile = 0;
            RFile = 0;
    }

        // матрица Q
        private void CreateQ()
        {
            // перввая строка
            // пробел и узлы
            // перебираем все узлы
            var line = new List<string> {""};
            line.AddRange(Nodes);

            // добавляем первую строку в матрицу Q
            Q.AddRange(new[] {line});

            
            // перебираем все элементы
            // заполняем матрицу одним элементом и значениями
            foreach (var e in Elems)
            {
                // очищаем строку
                line = new List<string> {e};

                // элемент

                // перебираем узлы
                for (var i = 0; i < Nodes.Count; i++)
                {
                    // наличие элемента в узле
                    line.Add(NodesElems[i].Contains(e) ? "1" : "0");
                }

                // добавляем строку в матрицу Q
                Q.AddRange(new[] { line });
            }

#if DEBUG
            Console.WriteLine(@"Q");
            foreach (var t in Q)
            {
                foreach (var e in t)
                {
                    Console.Write(@"    "+e);
                }
                Console.WriteLine();
            }
#endif
        }

        private void CheckBoxQ_Checked(object sender, RoutedEventArgs e)
        {
            var cb = (CheckBox)sender;

            if (cb.IsChecked == true)
            {
                ButtonSaveQ.IsEnabled = true;
                FileOutputQ.IsEnabled = true;
                Button.IsEnabled = true;
            }
            else
            {
                ButtonSaveQ.IsEnabled = false;
                FileOutputQ.IsEnabled = false;
                Button.IsEnabled = false;
            }
        }

        private void CheckBoxR_Checked(object sender, RoutedEventArgs e)
        {
            var cb = (CheckBox)sender;

            if (cb.IsChecked == true)
            {
                ButtonSaveR.IsEnabled = true;
                FileOutputR.IsEnabled = true;
                Button.IsEnabled = true;
            }
            else
            {
                ButtonSaveR.IsEnabled = false;
                FileOutputR.IsEnabled = false;
                Button.IsEnabled = false;
            }
        }
    }
}
