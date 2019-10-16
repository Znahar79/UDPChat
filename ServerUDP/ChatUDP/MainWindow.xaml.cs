using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using TimeMeasure = System.Timers;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ChatUDP
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    
    public partial class MainWindow : Window
    {
        bool alive = false; // будет ли работать поток для приема
        UdpClient client;
        int LOCALPORT; // порт для приема сообщений
        const int TTL = 20;// Time to live
        string HOST; // хост для групповой рассылки
        IPAddress groupAddress; // адрес для групповой рассылки
        //string userName;
        SynchronizationContext context;
        List<string> users_in_room = new List<string>();//пользователи в чате
        List<int> banned = new List<int>(); //забаненые никнеймы
        List<int> ports = new List<int>();// порты для отправки сообщений
        //TimeMeasure.Timer broadcast_timer = new TimeMeasure.Timer(100);

        //TimeMeasure.Timer recv_timer = new TimeMeasure.Timer(50);

        //void broadcast_by_time(Object source, TimeMeasure.ElapsedEventArgs e)
        //{
        //    string listofusers = "1 \n";
        //    foreach (string name in users_in_room)
        //    {
        //        listofusers += name + "\r\n";
        //    }
        //    byte[] data = Encoding.Unicode.GetBytes(listofusers);
        //    foreach(int port in ports)
        //    {
        //        client.Send(data, data.Length, HOST, port);
        //    }
        //}
        //void recv_by_time(Object source, TimeMeasure.ElapsedEventArgs e)
        //{
        //    ReceiveMessages();
        //}
        public MainWindow()
        {
            InitializeComponent();
            context = SynchronizationContext.Current;

            //broadcast_timer.Elapsed += broadcast_by_time;
            //recv_timer.Elapsed += recv_by_time;

            //broadcast_timer.Start();
            //recv_timer.Start();
        }
        
        //Запуск сервера
        private void SubmitionOfPortAndIP_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //broadcast_timer.Elapsed += broadcast_by_time;
                //recv_timer.Elapsed += recv_by_time;

                //broadcast_timer.Start();
                //recv_timer.Start();

                //ChatWindow chat = new ChatWindow();
                LOCALPORT = int.Parse(InputPort.Text);//OfUser.Text);
                //REMOTEPORT = int.Parse(InputPort.Text);
                HOST = InputIP.Text;
                groupAddress = IPAddress.Parse(HOST);
                //chat.ViewModel = "ViewModel";
                //chat.Show();
                //chat.ShowViewModel();
                IPEndPoint localpt = new IPEndPoint(IPAddress.Any, LOCALPORT);
                client = new UdpClient();
                client.Client.SetSocketOption(SocketOptionLevel.Socket,SocketOptionName.ReuseAddress, true);
                client.Client.Bind(localpt);

                // присоединяемся к групповой рассылке
                client.JoinMulticastGroup(groupAddress, TTL);

                // запускаем задачу на прием сообщений
                Task receiveTask = new Task(ReceiveMessages);
                receiveTask.Start();
               
                InputPort.IsReadOnly = true;
                InputIP.IsReadOnly = true;
                //InputPortOfUser.IsReadOnly = true;
                SubmitionOfPortAndIP.IsEnabled = false;
                Exit_Button.IsEnabled = true;
                Send_Button.IsEnabled = true;
                UserMessage.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        
        // метод приема сообщений
        private void ReceiveMessages()
        {
            alive = true;
            try
            {
                while (alive)
                {
                   
                    IPEndPoint remoteIp = null;
                    byte[] data = client.Receive(ref remoteIp);
                    string message = Encoding.Unicode.GetString(data);
                    if (message[0] == '0')
                    {
                        message = message.Substring(message.IndexOf("\n") + 1);
                        string time = DateTime.Now.ToShortTimeString();
                        context.Post(delegate (object state) { ChatMessages.AppendText(time + " " + message + "\r\n"); }, null);
                        string message_from_server = "0 \n";
                        message_from_server += ChatMessages;
                        foreach (int port in ports)
                        {
                            data = Encoding.Unicode.GetBytes(message_from_server);
                            client.Send(data, data.Length, HOST, port);
                        }
                    }
                    else if (message[0] == '1')
                    {
                        message = message.Substring(message.IndexOf("\n"));
                        users_in_room.Add(message);
                        ListOfUsers.Clear();
                        context.Post(delegate (object state) { ListOfUsers.AppendText(message + "\r\n"); }, null);
                        string message_from_server1 = "1 \n";
                        foreach(string name in users_in_room) { message_from_server1 += name + "\r\n"; }
                        foreach (int port in ports)
                        {
                            data = Encoding.Unicode.GetBytes(message_from_server1);
                            client.Send(data, data.Length, HOST, port);
                        }
                    }
                    else if (message[0] == 'p')
                    {
                        message = message.Substring(message.IndexOf("\n") + 1);
                        ports.Add(int.Parse(message));
                    }
                    else if (message[0] == 'e')
                    {
                        message = message.Substring(message.IndexOf("\n") + 1);
                        ports.Remove(int.Parse(message));
                    }
                    else if (message[0] == 'o')
                    {
                        message = message.Substring(message.IndexOf("\n") + 1);
                        users_in_room.Remove(message);
                        ListOfUsers.Clear();
                        context.Post(delegate (object state) { ListOfUsers.AppendText(message + "\r\n"); }, null);
                        string message_from_server1 = "1 \n";
                        foreach (string name in users_in_room) { message_from_server1 += name + "\r\n"; }
                        foreach (int port in ports)
                        {
                            data = Encoding.Unicode.GetBytes(message_from_server1);
                            client.Send(data, data.Length, HOST, port);
                        }
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                if (!alive)
                    return;
                throw;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        

        // выход из чата
        private void ExitChat()
        {
            client.DropMulticastGroup(groupAddress);

            alive = false;
            client.Close();

            SubmitionOfPortAndIP.IsEnabled = true;
            Exit_Button.IsEnabled = false;
            Send_Button.IsEnabled = false;
            UserMessage.IsEnabled = false;
            
            InputPort.IsReadOnly = false;
            InputIP.IsReadOnly = false;
        }
        private void LogoutButton_Click(object sender, EventArgs e)
        {
            ExitChat();
        }
        //Бан пользователей
        private void BanButton_Click(object sender, EventArgs e)
        {
            try
            {
                string message = "@/n";
                for(int i = 0; i < UserMessage.Text.Length - 1; i++)
                {
                    string Cur_us_name = String.Empty;
                    //Разделители пропускаем
                    if (IsDelimeter(UserMessage.Text[i]))
                        continue; //Переходим к следующему символу

                    if (Char.IsDigit(UserMessage.Text[i])) //Если буква
                    {
                        //Читаем до разделителя или оператора, что бы получить число
                        while (!IsDelimeter(UserMessage.Text[i]))
                        {
                            Cur_us_name += UserMessage.Text[i]; //Добавляем каждую цифру числа к нашей строке
                            i++; //Переходим к следующему символу

                            if (i == UserMessage.Text.Length-1) break; //Если символ - последний, то выходим из цикла
                        }
                    }
                    //ports.Remove(int.Parse(Cur_us_name));
                    banned.Add(int.Parse(Cur_us_name));
                    byte[] data = Encoding.Unicode.GetBytes(message);
                    client.Send(data, data.Length, HOST, int.Parse(Cur_us_name));
                    if (i == UserMessage.Text.Length - 1) break; //Если символ - последний, то выходим из цикла
                }
                // message += String.Format("{0}: {1}", userName, UserMessage.Text);
                //string message_after_del = "1 \n";
                //foreach (string name in users_in_room)
                //{
                //    message_after_del += name + "\r\n";
                //}
                //byte[] data = Encoding.Unicode.GetBytes(message_after_del);
                ////client.Send(data, data.Length, HOST, LOCALPORT);
                //foreach (int port in ports) {client.Send(data, data.Length, HOST, port);}         
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (alive)
                ExitChat();
        }

        //private void InputPort_TextChanged(object sender, TextChangedEventArgs e)
        //{

        //}

        static private bool IsDelimeter(char c)
        {
            if ((" =,:;".IndexOf(c) != -1))
                return true;
            return false;
        }

        
    }
}
