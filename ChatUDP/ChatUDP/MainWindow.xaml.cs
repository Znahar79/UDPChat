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
        int REMOTEPORT; // порт для отправки сообщений
        const int TTL = 20;// Time to live
        string HOST; // хост для групповой рассылки
        IPAddress groupAddress; // адрес для групповой рассылки
        string userName;
        SynchronizationContext context;
        //List<string> users_in_room = new List<string>();
        //List<string> banned = new List<string>();
        //int sen_counter = 0;
        public MainWindow()
        {
            InitializeComponent();
            context = SynchronizationContext.Current;

            //broadcast_timer.Elapsed += broadcast_by_time;
            //recv_timer.Elapsed += recv_by_time;

            //broadcast_timer.Start();
            //recv_timer.Start();
        }
        
        //Button btn = new Button();
        //btn.Name = "SubmitionOfPortAndIP";
        //btn.Click += btn1_Click;

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
                REMOTEPORT = 8001;
                HOST = InputIP.Text;
                groupAddress = IPAddress.Parse(HOST);
                userName = NicknameInput.Text;
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


                // отправляем первое сообщение о входе нового пользователя
                //отправка порта
                string message = "p \n";
                message += LOCALPORT + "\n";
                byte[] data = Encoding.Unicode.GetBytes(message);
                client.Send(data, data.Length, HOST, REMOTEPORT);
                //отправка имени пользователя
                message = "1 \n";
                message += userName + "\r\n";
                data = Encoding.Unicode.GetBytes(message);
                client.Send(data, data.Length, HOST, REMOTEPORT);
                
                //отправка собщения в чат
                message = "0 \n"; 
                message += userName + " вошел в чат";
                data = Encoding.Unicode.GetBytes(message);
                client.Send(data, data.Length, HOST, REMOTEPORT);
                //data = Encoding.Unicode.GetBytes(listofusers);
                //client.Send(data, data.Length, HOST, REMOTEPORT);
                NicknameInput.IsReadOnly = true;
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
                    }
                    else if (message[0] == '1')
                    {
                        //context.Post(delegate (object state) { ListOfUsers.Clear(); }, null);
                        message = message.Substring(message.IndexOf("\n"));
                        ListOfUsers.Clear();
                        context.Post(delegate (object state) { ListOfUsers.AppendText( message + "\r\n" ); }, null);
                    }
                    else if (message[0] == '@')
                    {
                        ExitChat();
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
            string message = "e \n";
            message += LOCALPORT;
            byte[] data = Encoding.Unicode.GetBytes(message);
            client.Send(data, data.Length, HOST, REMOTEPORT);
            //отправка имени пользователя
            message = "o \n";
            message += userName + "\r\n";
            data = Encoding.Unicode.GetBytes(message);
            client.Send(data, data.Length, HOST, REMOTEPORT);

            message = "0 \n";
            message += userName + " покидает чат";
            data = Encoding.Unicode.GetBytes(message);
            client.Send(data, data.Length, HOST, REMOTEPORT);
            client.DropMulticastGroup(groupAddress);

            alive = false;
            client.Close();

            SubmitionOfPortAndIP.IsEnabled = true;
            Exit_Button.IsEnabled = false;
            Send_Button.IsEnabled = false;
            UserMessage.IsEnabled = false;
            NicknameInput.IsReadOnly = false;
            InputPort.IsReadOnly = false;
            InputIP.IsReadOnly = false;
        }
        private void LogoutButton_Click(object sender, EventArgs e)
        {
            ExitChat();
        }

        private void SendButton_Click(object sender, EventArgs e)
        {
            try
            {
                string message = "0 \n";
                message += String.Format("{0}: {1}", userName, UserMessage.Text);
                byte[] data = Encoding.Unicode.GetBytes(message);
                //client.Send(data, data.Length, HOST, LOCALPORT);
                client.Send(data, data.Length, HOST, REMOTEPORT);
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
    }
}
