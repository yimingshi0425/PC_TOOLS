using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.IO.Ports;
using System.Management;
using System.Collections;
using System.IO;

namespace BCIM_Tool
{
    public partial class MainWindow : Window
    {
        SerialPort serialPort = new SerialPort();
        int[] TxBuf_34H_2024JL = { 0x55, 0xB4, 0x55, 0xD0, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x25 };
        Byte[] frameID = { 0x34 };

        Byte[] buffer = new byte[11];/*Read Data*/
        bool ledState_1 = true, ledState_2 = true, ledState_3 = true, ledState_4 = true, ledState_5 = true;/*LED 狀態*/

        bool startStatus = false;/*傳送命令是否啟動狀態狀態*/
        bool readDataStatus = true;/*讀取資料是否啟動狀態狀態*/
        int blinkFlag = 0, blinkRate = 100;

        int underVoltage = 0, overTemperature = 0, parityError = 0, checkSumError = 0, dataError = 0, responseError = 0;

        int rr = 0, gg = 0, br = 0, bg = 0, bb = 0;/*LED VF Parameter*/
        int rr_min = 1000, gg_min = 1000, br_min = 1000, bg_min = 1000, bb_min = 1000;
        int rr_max = 0, gg_max = 0, br_max = 0, bg_max = 0, bb_max = 0;      
        int VF = 0, ledFail_1 = 0, ledFail_2 = 0, ledFail_3 = 0, ledFail_4 = 0;
        string send = "", read = "", error = "";
        int sendConut = 0, readCount = 0, timeoutCount = 0;
        bool ledFailStatus = false;
        int temp = 0;

        byte[] output = new byte[11];
        string[] hexString;
        string checksumMode = "Enhanced";
        bool watdogStart = false;

        public MainWindow()
        {
            InitializeComponent();
            BT_start_stop.IsEnabled = false;
            CB_manual_mode.Visibility = Visibility.Hidden;

            LED_1.Fill = Brushes.Green;
            LED_1.Stroke = Brushes.Green;
            LED_2.Fill = Brushes.Green;
            LED_2.Stroke = Brushes.Green;
            LED_3.Fill = Brushes.Green;
            LED_3.Stroke = Brushes.Green;
            LED_4.Fill = Brushes.Green;
            LED_4.Stroke = Brushes.Green;
            LED_5.Fill = Brushes.White;
            LED_5.Stroke = Brushes.White;

            LedFail_5.Visibility = Visibility.Hidden;

            CB_error_test_mode.Items.Add("Normal");
            CB_error_test_mode.Items.Add("Parity Error");
            CB_error_test_mode.Items.Add("Checksum Error");
            CB_error_test_mode.Items.Add("Data Error");
            CB_error_test_mode.SelectedIndex = 0;

            CB_color.Items.Add("Green");
            CB_color.Items.Add("Blue");
            CB_color.Items.Add("Red");
            CB_color.SelectedIndex = 0;

            CB_blink_mode.Items.Add("No Blink");
            CB_blink_mode.Items.Add("Blink Rate 1");
            CB_blink_mode.Items.Add("Blink Rate 3");
            CB_blink_mode.SelectedIndex = 0;

            CB_model.Items.Add("2020JL BCIM");
            CB_model.Items.Add("2024JL BCIM");
            CB_model.SelectedIndex = 1;

            CB_diagnostic_command.Items.Add("55 3C 62 06 C0 FF FF FF FF FF");
            CB_diagnostic_command.Items.Add("55 3C 62 06 C7 FF FF FF FF FF");
            CB_diagnostic_command.Items.Add("55 3C 62 06 C8 FF FF FF FF FF");
            CB_diagnostic_command.Items.Add("55 B4 55 D0 00 FF FF FF FF FF");           
            CB_diagnostic_command.SelectedIndex = 3;

            CB_led5_blink.Visibility = Visibility.Hidden;



            string[] ports = SerialPort.GetPortNames();
            foreach (string port in ports)
            {
                CB_port.Items.Add(port);
            }
            serialPort.DataReceived += SerialPort_DataReceived;
            

            Task.Run(() => SendCommand());
            Task.Run(() => UIUpdate());
            Task.Run(() => TimeOutWatchDog());


        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                startStatus = false;
                ClosePort();
            }
            catch (Exception)
            {

            }
        }

        /*傳送命令*/
        public async Task SendCommand()
        {
            for (; ; )
            {
                if (startStatus)
                {
                    TxBuf_34H_2024JL[0] = 0x55;//同步碼
                    TxBuf_34H_2024JL[1] = 0xb4;//PID
                    TxBuf_34H_2024JL[2] = 0x00;//SET IND1~4
                    TxBuf_34H_2024JL[3] = 0xC0;//SET IND5，color
                    TxBuf_34H_2024JL[4] = 0x00;//SET flash rate
                    TxBuf_34H_2024JL[5] = 0xFF;
                    TxBuf_34H_2024JL[6] = 0xFF;
                    TxBuf_34H_2024JL[7] = 0xFF;
                    TxBuf_34H_2024JL[8] = 0xFF;

                    Dispatcher.Invoke(new Action(() =>
                    {
                        if(CB_led1_blink.IsChecked == true)
                        {
                            TxBuf_34H_2024JL[2] = TxBuf_34H_2024JL[2] | (0x02 << 0);
                        }
                        else
                        {
                            if (ledState_1)
                            {
                                TxBuf_34H_2024JL[2] = TxBuf_34H_2024JL[2] | (0x01 << 0);
                            }
                            else
                            {
                                TxBuf_34H_2024JL[2] = TxBuf_34H_2024JL[2] | (0x00 << 0);
                            }
                        }

                        if (CB_led2_blink.IsChecked == true)
                        {
                            TxBuf_34H_2024JL[2] = TxBuf_34H_2024JL[2] | (0x02 << 2);
                        }
                        else
                        {
                            if (ledState_2)
                            {
                                TxBuf_34H_2024JL[2] = TxBuf_34H_2024JL[2] | (0x01 << 2);
                            }
                            else
                            {
                                TxBuf_34H_2024JL[2] = TxBuf_34H_2024JL[2] | (0x00 << 2);
                            }
                        }

                        if (CB_led3_blink.IsChecked == true)
                        {
                            TxBuf_34H_2024JL[2] = TxBuf_34H_2024JL[2] | (0x02 << 4);
                        }
                        else
                        {
                            if (ledState_3)
                            {
                                TxBuf_34H_2024JL[2] = TxBuf_34H_2024JL[2] | (0x01 << 4);
                            }
                            else
                            {
                                TxBuf_34H_2024JL[2] = TxBuf_34H_2024JL[2] | (0x00 << 4);
                            }
                        }

                        if (CB_led4_blink.IsChecked == true)
                        {
                            TxBuf_34H_2024JL[2] = TxBuf_34H_2024JL[2] | (0x02 << 6);
                        }
                        else
                        {
                            if (ledState_4)
                            {
                                TxBuf_34H_2024JL[2] = TxBuf_34H_2024JL[2] | (0x01 << 6);
                            }
                            else
                            {
                                TxBuf_34H_2024JL[2] = TxBuf_34H_2024JL[2] | (0x00 << 6);
                            }
                        }

                        if (ledFailStatus)
                        {
                            if (CB_fail_led_off.IsChecked == true)
                            {
                                TxBuf_34H_2024JL[2] = 0;
                            }
                        }
                        
                        switch (CB_color.SelectedIndex)
                        {
                            case 0://Green
                                TxBuf_34H_2024JL[3] = TxBuf_34H_2024JL[3] | (0x02 << 3);
                                break;
                            case 1://Blue
                                TxBuf_34H_2024JL[3] = TxBuf_34H_2024JL[3] | (0x03 << 3);
                                break;
                            case 2://Red
                                TxBuf_34H_2024JL[3] = TxBuf_34H_2024JL[3] | (0x01 << 3);
                                break;
                        }

                        switch (CB_blink_mode.SelectedIndex)
                        {
                            case 0:
                                TxBuf_34H_2024JL[4] = 0;
                                break;
                            case 1:
                                TxBuf_34H_2024JL[4] = 1;
                                break;
                            case 2:
                                TxBuf_34H_2024JL[4] = 3;
                                break;
                        }

                        /*這個switch函數必須放最後因為裡面有checksum計算函數*/
                        switch (CB_error_test_mode.SelectedIndex)
                        {
                            /*Normal*/
                            case 0:
                                TxBuf_34H_2024JL[1] = 0xb4;
                                TxBuf_34H_2024JL[9] = 0xff;
                                TxBuf_34H_2024JL[10] = CalCheckSum(0xb4, true);
                                break;
                            /*Parity Error*/
                            case 1:
                                TxBuf_34H_2024JL[1] = 0x34;
                                TxBuf_34H_2024JL[9] = 0xff;
                                TxBuf_34H_2024JL[10] = CalCheckSum(0x34, true);
                                break;
                            /*Checksum Error*/
                            case 2:
                                TxBuf_34H_2024JL[1] = 0xb4;
                                TxBuf_34H_2024JL[9] = 0xff;
                                TxBuf_34H_2024JL[10] = 0xFF;
                                break;
                            /*Data Error*/
                            case 3:
                                TxBuf_34H_2024JL[1] = 0xb4;
                                TxBuf_34H_2024JL[9] = 0xff;
                                TxBuf_34H_2024JL[10] = CalCheckSum(0xb4, true);

                                /*製造Data Error用*/
                                TxBuf_34H_2024JL[9] = 0x02;
                                break;
                        }
                    }));

                    /*傳送命令*/
                    if (serialPort.IsOpen)
                    {
                        //Console.WriteLine("Send Command");

                        byte[] test = new byte[11];
                        for (int i = 0; i < 11; i++)
                        {
                            test[i] = Convert.ToByte(TxBuf_34H_2024JL[i]);
                        }

                        //foreach (int i in test)
                        //{
                        //    Console.Write(i + " ");
                        //}
                        //Console.WriteLine();

                        serialPort.Write(test, 0, 11);
                        sendConut += 1;
                        watdogStart = true;
                    }

                    send = "";
                    for (int i = 0; i < 11; i++)
                    {
                        send += TxBuf_34H_2024JL[i].ToString("X2");
                        send += " ";
                    }
                    Console.WriteLine("TX => " + send);

                    Dispatcher.Invoke(new Action(() =>
                    {
                        if (RB_roll_mode.IsChecked == true)
                        {
                            TB_monitor.AppendText(GetTime() + "\tS => " + send + "\n");
                            TB_monitor.ScrollToEnd();
                        }
                        else if (RB_update_mode.IsChecked == true)
                        {
                            TB_monitor.Text = GetTime() + "\tS => " + send + "\n";
                        }
                        TB_emc_monitor.AppendText(GetTime() + "\tS => " + send + "\n");
                        TB_emc_monitor.ScrollToEnd();

                        LB_send_count.Content = "TX Count => " + sendConut.ToString();
                        WriteToFile(DateTime.Now + " Send => " + send + "\n");
                    }));
                }
                await Task.Delay(200);
            }
        }

        /*接收訊號Timeout看門狗*/
        public async Task TimeOutWatchDog()
        {
            for(; ; )
            {
                try
                {
                    if (watdogStart)
                    {
                        //Console.WriteLine(DateTime.Now.Second + "." + DateTime.Now.Millisecond);
                        await Task.Delay(100);
                        if (watdogStart)
                        {
                            Console.WriteLine("Timeout");
                            timeoutCount++;
                        }                                           
                    }
                    else
                    {
                        
                    }
                   
                }
                catch (Exception)
                {

                }
                
            }
        }

        /*更新UI*/
        public async Task UIUpdate()
        {
            for ( ; ; )
            {
                Dispatcher.Invoke(new Action(() =>
                {                 
                    switch (CB_color.SelectedIndex)
                    {
                        case 0:
                            if (ledState_1)
                            {
                                LED_1.Fill = Brushes.Green;
                                LED_1.Stroke = Brushes.Green;
                            }
                            else
                            {
                                LED_1.Fill = Brushes.Black;
                                LED_1.Stroke = Brushes.Black;
                            }

                            if (ledState_2)
                            {
                                LED_2.Fill = Brushes.Green;
                                LED_2.Stroke = Brushes.Green;
                            }
                            else
                            {
                                LED_2.Fill = Brushes.Black;
                                LED_2.Stroke = Brushes.Black;
                            }

                            if (ledState_3)
                            {
                                LED_3.Fill = Brushes.Green;
                                LED_3.Stroke = Brushes.Green;
                            }
                            else
                            {
                                LED_3.Fill = Brushes.Black;
                                LED_3.Stroke = Brushes.Black;
                            }

                            if (ledState_4)
                            {
                                LED_4.Fill = Brushes.Green;
                                LED_4.Stroke = Brushes.Green;
                            }
                            else
                            {
                                LED_4.Fill = Brushes.Black;
                                LED_4.Stroke = Brushes.Black;
                            }
                            break;
                        case 1:
                            if (ledState_1)
                            {
                                LED_1.Fill = Brushes.Blue;
                                LED_1.Stroke = Brushes.Blue;
                            }
                            else
                            {
                                LED_1.Fill = Brushes.Black;
                                LED_1.Stroke = Brushes.Black;
                            }

                            if (ledState_2)
                            {
                                LED_2.Fill = Brushes.Blue;
                                LED_2.Stroke = Brushes.Blue;
                            }
                            else
                            {
                                LED_2.Fill = Brushes.Black;
                                LED_2.Stroke = Brushes.Black;
                            }

                            if (ledState_3)
                            {
                                LED_3.Fill = Brushes.Blue;
                                LED_3.Stroke = Brushes.Blue;
                            }
                            else
                            {
                                LED_3.Fill = Brushes.Black;
                                LED_3.Stroke = Brushes.Black;
                            }

                            if (ledState_4)
                            {
                                LED_4.Fill = Brushes.Blue;
                                LED_4.Stroke = Brushes.Blue;
                            }
                            else
                            {
                                LED_4.Fill = Brushes.Black;
                                LED_4.Stroke = Brushes.Black;
                            }
                            break;
                        case 2:
                            if (ledState_1)
                            {
                                LED_1.Fill = Brushes.Red;
                                LED_1.Stroke = Brushes.Red;
                            }
                            else
                            {
                                LED_1.Fill = Brushes.Black;
                                LED_1.Stroke = Brushes.Black;
                            }

                            if (ledState_2)
                            {
                                LED_2.Fill = Brushes.Red;
                                LED_2.Stroke = Brushes.Red;
                            }
                            else
                            {
                                LED_2.Fill = Brushes.Black;
                                LED_2.Stroke = Brushes.Black;
                            }

                            if (ledState_3)
                            {
                                LED_3.Fill = Brushes.Red;
                                LED_3.Stroke = Brushes.Red;
                            }
                            else
                            {
                                LED_3.Fill = Brushes.Black;
                                LED_3.Stroke = Brushes.Black;
                            }

                            if (ledState_4)
                            {
                                LED_4.Fill = Brushes.Red;
                                LED_4.Stroke = Brushes.Red;
                            }
                            else
                            {
                                LED_4.Fill = Brushes.Black;
                                LED_4.Stroke = Brushes.Black;
                            }
                            break;
                    }

                    LB_timeout_count.Content = timeoutCount.ToString();
                    LB_send_checksum.Content = output[10].ToString("X2");
                }));
                await Task.Delay(100);
            }
        }

        /*接收訊號*/
        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            watdogStart = false;
            serialPort.Read(buffer, 0, 11);
                        
            //foreach (int i in buffer)
            //{
            //    Console.Write(i.ToString("x2") + " ");
            //}
            //Console.WriteLine();

            read = "";
            for (int i = 0; i < 11; i++)
            {
                read += buffer[i].ToString("X2");
                read += " ";
            }
            //Console.WriteLine("R => " + read);

            
            Dispatcher.Invoke(new Action(() =>
            {
                if (serialPort.IsOpen)
                {
                    /*When "readDataStatus" == true => "No Monitor" is Checked*/
                    if (readDataStatus)
                    {
                        ErrorTest();

                        if (RB_roll_mode.IsChecked == true)
                        {
                            TB_monitor.AppendText(GetTime() + "\tR => " + read + error + "\n");
                            TB_monitor.ScrollToEnd();
                        }
                        else if (RB_update_mode.IsChecked == true)
                        {
                            TB_monitor.Text = GetTime() + "\tR => " + read + error + "\n";
                        }
                        TB_emc_monitor.AppendText(GetTime() + "\tR => " + read + error + "\n");
                        TB_emc_monitor.ScrollToEnd();

                        ShowLEDFail();
                    }
                    
                    ShowLEDVF();
                                                          
                    LB_read_count.Content = "RX Count => " + readCount.ToString();
                    /*檔案寫入儲存*/ 
                    WriteToFile(DateTime.Now + " Read => " + read + "\n");
                }
            }));
        }

        /*擷取目前時間*/
        public string GetTime()
        {
            string time = DateTime.Now.Year + "/" + DateTime.Now.Month + "/" + DateTime.Now.Day + " "
                        + DateTime.Now.Hour + ":" + DateTime.Now.Minute + ":" 
                        + DateTime.Now.Second + ":" + DateTime.Now.Millisecond;
            return time;
        }

        /*Error Test*/
        public void ErrorTest()
        {
            error = "";

            underVoltage = buffer[6] & 0x01;
            overTemperature = buffer[6] & 0x02;
            parityError = buffer[6] & 0x04;
            checkSumError = buffer[6] & 0x08;
            dataError = buffer[6] & 0x10;
            responseError = buffer[6] & 0x20;

            //Console.WriteLine("UnderVoltage => " + underVoltage);
            //Console.WriteLine("OverTemperature => " + overTemperature);
            //Console.WriteLine("Checksum => " + checkSumError);
            //Console.WriteLine("Data => " + dataError);
            //Console.WriteLine("Response => " + responseError);

            if (underVoltage == 1)
            {
                ERR_undervoltage.Fill = Brushes.Red;
                ERR_undervoltage.Stroke = Brushes.Red;
            }
            else
            {
                ERR_undervoltage.Fill = Brushes.Black;
                ERR_undervoltage.Stroke = Brushes.Black;
            }

            if (overTemperature == 2)
            {
                ERR_over_temperature.Fill = Brushes.Red;
                ERR_over_temperature.Stroke = Brushes.Red;
            }
            else
            {
                ERR_over_temperature.Fill = Brushes.Black;
                ERR_over_temperature.Stroke = Brushes.Black;
            }

            if (responseError == 32)
            {
                ERR_response.Fill = Brushes.Red;
                ERR_response.Stroke = Brushes.Red;
                error = "=>Response Error";
            }
            else
            {
                ERR_response.Fill = Brushes.Black;
                ERR_response.Stroke = Brushes.Black;
            }

            if (parityError == 4)
            {
                ERR_parity.Fill = Brushes.Red;
                ERR_parity.Stroke = Brushes.Red;
                error = "=>Parity Error";
            }
            else
            {
                ERR_parity.Fill = Brushes.Black;
                ERR_parity.Stroke = Brushes.Black;
            }

            if (checkSumError == 8)
            {
                ERR_checksum.Fill = Brushes.Red;
                ERR_checksum.Stroke = Brushes.Red;
                error = "=>CheckSum Error";
            }
            else
            {
                ERR_checksum.Fill = Brushes.Black;
                ERR_checksum.Stroke = Brushes.Black;
            }

            if (dataError == 16)
            {
                ERR_data.Fill = Brushes.Red;
                ERR_data.Stroke = Brushes.Red;
                error = "=>Data Error";
            }
            else
            {
                ERR_data.Fill = Brushes.Black;
                ERR_data.Stroke = Brushes.Black;
            }

            if (buffer[2] == 0)
            {
                error = "=>Response Error";
            }

            if (error == "")
            {
                readCount += 1;
            }
        }

        /*顯示LED錯誤*/
        public void ShowLEDFail()
        {
            /*LED NORMAL 黑色 0*/
            /*LED OPEN   紅色 1*/
            /*LED SHORT  綠色 2*/

            bool f1 = false, f2 = false, f3 = false, f4 = false;

            /*Led 1*/           
            ledFail_1 = buffer[4] & 0x03;
            LB_led1_fail_code.Content = ledFail_1.ToString();
            switch (ledFail_1)
            {
                case 0:
                    LedFail_1.Fill = Brushes.Black;
                    LedFail_1.Stroke = Brushes.Black;
                    LB_led1_fail_description.Content = "Normal";
                    f1 = false;
                    break;
                case 1:
                    LedFail_1.Fill = Brushes.Red;
                    LedFail_1.Stroke = Brushes.Red;
                    LB_led1_fail_description.Content = "Open";
                    f1 = true;
                    break;
                case 2:
                    LedFail_1.Fill = Brushes.Green;
                    LedFail_1.Stroke = Brushes.Green;
                    LB_led1_fail_description.Content = "Short";
                    f1 = true;
                    break;
            }

            /*Led 2*/
            ledFail_2 = (buffer[4] >> 2) & 0x03;
            LB_led2_fail_code.Content = ledFail_2.ToString();
            switch (ledFail_2)
            {
                case 0:
                    LedFail_2.Fill = Brushes.Black;
                    LedFail_2.Stroke = Brushes.Black;
                    LB_led2_fail_description.Content = "Normal";
                    f2 = false;
                    break;
                case 1:
                    LedFail_2.Fill = Brushes.Red;
                    LedFail_2.Stroke = Brushes.Red;
                    LB_led2_fail_description.Content = "Open";
                    f2 = true;
                    break;
                case 2:
                    LedFail_2.Fill = Brushes.Green;
                    LedFail_2.Stroke = Brushes.Green;
                    LB_led2_fail_description.Content = "Short";
                    f2 = true;
                    break;
            }

            /*Led 3*/
            ledFail_3 = (buffer[4] >> 4) & 0x03;
            LB_led3_fail_code.Content = ledFail_3.ToString();
            switch (ledFail_3)
            {
                case 0:
                    LedFail_3.Fill = Brushes.Black;
                    LedFail_3.Stroke = Brushes.Black;
                    LB_led3_fail_description.Content = "Normal";
                    f3 = false;
                    break;
                case 1:
                    LedFail_3.Fill = Brushes.Red;
                    LedFail_3.Stroke = Brushes.Red;
                    LB_led3_fail_description.Content = "Open";
                    f3 = true;
                    break;
                case 2:
                    LedFail_3.Fill = Brushes.Green;
                    LedFail_3.Stroke = Brushes.Green;
                    LB_led3_fail_description.Content = "Short";
                    f3 = true;
                    break;
            }

            /*Led 4*/
            ledFail_4 = (buffer[4] >> 6) & 0x03;
            LB_led4_fail_code.Content = ledFail_4.ToString();
            switch (ledFail_4)
            {
                case 0:
                    LedFail_4.Fill = Brushes.Black;
                    LedFail_4.Stroke = Brushes.Black;
                    LB_led4_fail_description.Content = "Normal";
                    f4 = false;
                    break;
                case 1:
                    LedFail_4.Fill = Brushes.Red;
                    LedFail_4.Stroke = Brushes.Red;
                    LB_led4_fail_description.Content = "Open";
                    f4 = true;
                    break;
                case 2:
                    LedFail_4.Fill = Brushes.Green;
                    LedFail_4.Stroke = Brushes.Green;
                    LB_led4_fail_description.Content = "Short";
                    f4 = true;
                    break;
            }

            if (f1 == true | f2 == true | f3 == true | f4 == true)
            {
                ledFailStatus = true;                
            }
            else
            {
                ledFailStatus = false;
            }
            buffer[4] = 0x00;
        }

        /*顯示LED VF*/
        public void ShowLEDVF()
        {
            if(CB_vf_monitor.IsChecked == true)
            {
                VF = (buffer[8] << 8) + buffer[9];

                if (buffer[7] == 0x11)
                {
                    rr = VF;
                    rr_min = Math.Min(rr, rr_min);
                    rr_max = Math.Max(rr, rr_max);

                    LB_RR.Content = VF.ToString();
                    LB_RR_min.Content = rr_min.ToString();
                    LB_RR_max.Content = rr_max.ToString();
                    //Console.WriteLine("RR => " + VF);
                }
                else if (buffer[7] == 0x22)
                {
                    gg = VF;
                    gg_min = Math.Min(gg, gg_min);
                    gg_max = Math.Max(gg, gg_max);

                    LB_GG.Content = VF.ToString();
                    LB_GG_min.Content = gg_min.ToString();
                    LB_GG_max.Content = gg_max.ToString();
                    //Console.WriteLine("GG => " + VF);
                }
                else if (buffer[7] == 0x31)
                {
                    br = VF;
                    br_min = Math.Min(br, br_min);
                    br_max = Math.Max(br, br_max);

                    LB_BR.Content = VF.ToString();
                    LB_BR_min.Content = br_min.ToString();
                    LB_BR_max.Content = br_max.ToString();
                    //Console.WriteLine("BR => " + VF);
                }
                else if (buffer[7] == 0x32)
                {
                    bg = VF;
                    bg_min = Math.Min(bg, bg_min);
                    bg_max = Math.Max(bg, bg_max);

                    LB_BG.Content = VF.ToString();
                    LB_BG_min.Content = bg_min.ToString();
                    LB_BG_max.Content = bg_max.ToString();
                    //Console.WriteLine("BG => " + VF);
                }
                else if (buffer[7] == 0x33)
                {
                    bb = VF;
                    bb_min = Math.Min(bb, bb_min);
                    bb_max = Math.Max(bb, bb_max);

                    LB_BB.Content = VF.ToString();
                    LB_BB_min.Content = bb_min.ToString();
                    LB_BB_max.Content = bb_max.ToString();
                    //Console.WriteLine("BB => " + VF);
                }
            }            
        }

        /*顯示LED溫度*/
        public void ShowLEDTemperature()
        {
            if (CB_temperature_monitor.IsChecked == true)
            {
                var temperature_avg = buffer[7];
                //var temperature_led1 = buffer[7];
                var temperature_led2 = buffer[8];
                var temperature_led3 = buffer[9];
                //var temperature_led4 = buffer[7];
                //var temperature_led5 = buffer[7];
            }
        }

        /*Bit Array轉換Byte*/
        public byte ConvertToByte(BitArray bits)
        {
            if (bits.Count != 8)
            {
                throw new ArgumentException("bits");
            }
            byte[] bytes = new byte[1];
            bits.CopyTo(bytes, 0);
            return bytes[0];
        }

        /*計算PID*/
        public byte CalPID(byte[] frameID)
        {
            BitArray bits = new BitArray(frameID);
            //for(int i=7; i>=0; i--)
            //{
            //    Console.Write(Convert.ToInt32(bits[i]));
            //}
            //Console.WriteLine();
            //Console.WriteLine(Convert.ToInt32(bits[0] ^ bits[1] ^ bits[2] ^ bits[4]));
            //Console.WriteLine(Convert.ToInt32(!(bits[1] ^ bits[3] ^ bits[4] ^ bits[5])));

            bits[6] = bits[0] ^ bits[1] ^ bits[2] ^ bits[4];
            bits[7] = !(bits[1] ^ bits[3] ^ bits[4] ^ bits[5]);
            //for (int i = 7; i >= 0; i--)
            //{
            //    Console.Write(Convert.ToInt32(bits[i]));
            //}
            //Console.WriteLine();
            //Console.WriteLine(ConvertToByte(bits));

            return ConvertToByte(bits);
        }

        /*計算Checksum*/
        public byte CalCheckSum(int PID, bool enhanced)
        {
            if (enhanced)
            {
                int result = PID;

                for (int i = 0; i <= 7; i++)
                {
                    result += TxBuf_34H_2024JL[i + 2];

                    if (result > 255)
                    {
                        result = (result & 0xFF) + 1;
                    }
                }
                result = (-result - 1) & 0xFF;
                return Convert.ToByte(result);
            }
            else
            {
                int result = 0;

                /*ID=>3C 3D*/
                for (int i = 0; i < 8; i++)
                {
                    result += TxBuf_34H_2024JL[i + 2];

                    if (result > 255)
                    {
                        result = (result & 0xFF) + 1;
                    }
                }
                result = (-result - 1) & 0xFF;
                return Convert.ToByte(result);
            }            
        }

        /*計算Checksum*/
        public byte CalCheckSum(int PID, int[] data, bool enhanced)
        {
            if (enhanced)
            {
                int result = PID;

                for (int i = 0; i <= 7; i++)
                {
                    result += data[i + 2];

                    if (result > 255)
                    {
                        result = (result & 0xFF) + 1;
                    }
                }
                result = (-result - 1) & 0xFF;
                return Convert.ToByte(result);
            }
            else
            {
                int result = 0;

                for (int i = 0; i < 8; i++)
                {
                    result += data[i + 2];

                    if (result > 255)
                    {
                        result = (result & 0xFF) + 1;
                    }
                }
                result = (-result - 1) & 0xFF;
                return Convert.ToByte(result);
            }
        }

        /*Log紀錄*/
        public void WriteToFile(string Message)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + "\\Logs";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            //string filepath = AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\ServiceLog_" + DateTime.Now.Date.ToShortDateString().Replace('/', '_') + ".txt";
            string filepath = AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\Log_" + DateTime.Now.Date.ToString("yyyy_MM_dd") + ".txt";
            if (!File.Exists(filepath))
            {
                // Create a file to write to.   
                using (StreamWriter sw = File.CreateText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }
            else
            {
                using (StreamWriter sw = File.AppendText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }
        }

        /*開啟串口*/
        public void OpenPort()
        {
            try
            {
                if (serialPort.IsOpen)
                {
                    serialPort.Close();
                }
                serialPort.BaudRate = 19200;
                serialPort.DataBits = 8;
                serialPort.Parity = Parity.None;
                serialPort.StopBits = StopBits.One;

                serialPort.Open();
                if (serialPort.IsOpen)
                {
                    Console.WriteLine("Open Port => " + serialPort.PortName);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }

        /*關閉串口*/
        public void ClosePort()
        {
            try
            {
                if (serialPort.IsOpen)
                {
                    serialPort.Close();
                }
                if (!serialPort.IsOpen)
                {
                    Console.WriteLine("Close Port => " + serialPort.PortName);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void CB_port_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (CB_port.SelectedItem != null)
                {
                    serialPort.PortName = CB_port.SelectedItem.ToString();
                    Console.WriteLine("Select => " + CB_port.SelectedItem);
                    BT_start_stop.IsEnabled = true;
                    OpenPort();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void CB_port_DropDownOpened(object sender, EventArgs e)
        {
            try
            {
                CB_port.Items.Clear();
                string[] ports = SerialPort.GetPortNames();
                foreach (string port in ports)
                {
                    CB_port.Items.Add(port);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void CB_error_test_mode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {

            }
            catch (Exception)
            {

            }
        }

        private void CB_color_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                
            }
            catch (Exception)
            {

            }
        }

        private void CB_model_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (CB_model.SelectedIndex)
            {
                case 0:
                    LED_5.Fill = Brushes.Green;
                    LED_5.Stroke = Brushes.Green;
                    LedFail_5.Fill = Brushes.Black;
                    LedFail_5.Stroke = Brushes.Black;
                    CB_led5_blink.Visibility = Visibility.Visible;
                    LedFail_5.Visibility = Visibility.Visible;
                    break;
                case 1:
                    LED_5.Fill = Brushes.White;
                    LED_5.Stroke = Brushes.White;
                    LedFail_5.Fill = Brushes.White;
                    LedFail_5.Stroke = Brushes.White;
                    CB_led5_blink.Visibility = Visibility.Hidden;
                    LedFail_5.Visibility = Visibility.Hidden;
                    break;
            }
        }

        private void CB_blink_mode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                
            }
            catch (Exception)
            {

            }
        }

        private void CB_diagnostic_command_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                TB_send_command.Text = CB_diagnostic_command.SelectedItem.ToString();
            }
            catch (Exception)
            {

            }
        }

        private void CP_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            Console.Write(CP.SelectedColorText + " => ");
            Console.WriteLine(e.NewValue);//0XFFFFFFFF

        }

        private void CB_no_monitor_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (CB_no_monitor.IsChecked == true)
                {
                    readDataStatus = false;
                }
                else
                {
                    readDataStatus = true;
                }

            }
            catch (Exception)
            {

            }
        }

        private void CB_manual_mode_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (startStatus)
                {
                    startStatus = false;
                    BT_start_stop.Content = "Start";
                }
                else
                {
                    startStatus = true;
                    BT_start_stop.Content = "Stop";
                }                
            }
            catch (Exception)
            {
                
            }
        }

        private void TB_send_command_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                send = "";
                var inputStr = TB_send_command.Text;
                hexString = inputStr.Split(' ');
                int[] temp = new int[11];

                //for (int i = 0; i < hexString.Length; i++)
                //{
                //    Console.Write(hexString[i]);
                //    Console.Write(" ");
                //}
                //Console.WriteLine();
                //Console.WriteLine("-----------------------------------------------");

                for (int i = 0; i < hexString.Length; i++)
                {
                    send += hexString[i] + " ";
                    temp[i] = Convert.ToByte(hexString[i], 16);
                }
                
                if(hexString[1] == "3C")
                {
                    /*Classic Checksum*/
                    temp[10] = CalCheckSum(0xB4, temp, false);
                    checksumMode = "Classic";                    
                }
                else
                {
                    /*Enhanced Checksum*/
                    temp[10] = CalCheckSum(0xB4, temp, true);
                    checksumMode = "Enhanced";
                }
                Console.WriteLine(checksumMode);

                for (int i = 0; i < output.Length; i++)
                {
                    output[i] = Convert.ToByte(temp[i]);
                    //Console.Write(output[i].ToString("X2"));
                    //Console.Write(" ");
                }
                send += output[10].ToString("X2");
                //Console.WriteLine();
                //Console.WriteLine("-----------------------------------------------");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void BT_start_stop_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (startStatus)
                {
                    startStatus = false;
                    watdogStart = false;
                    BT_start_stop.Content = "Start";
                    if (dataError == 16)
                    {
                        error = "=>Data Error";
                        LB_mcode.Content = "M3";
                    }
                    else
                    {
                        LB_mcode.Content = "M1";
                    }
                }
                else
                {
                    //OpenPort();
                    startStatus = true;
                    if (serialPort.IsOpen)
                    {
                        BT_start_stop.Content = "Stop";
                    }
                    LB_mcode.Content = "";
                }
            }
            catch (Exception)
            {

            }
        }

        private void BT_clear_message_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TB_monitor.Text = "";
            }
            catch (Exception)
            {

            }
        }

        private void BT_send_command_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (serialPort.IsOpen)
                {
                    if(checksumMode == "Enhanced")
                    {
                        serialPort.Write(output, 0, output.Length);
                    }
                    else if(checksumMode == "Classic")
                    {
                        //byte[] t = { 0x3D };
                        serialPort.Write(output, 0, output.Length);
                        //serialPort.Write(t, 0, t.Length);
                    }
                    
                    sendConut += 1;
                    LB_send_count.Content = "TX Count => " + sendConut.ToString();
                    TB_monitor.AppendText(GetTime() + "\tS => " + send + "\n");
                    TB_monitor.ScrollToEnd();
                }
            }
            catch (Exception)
            {

            }
        }

        private void LED_1_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (!ledState_1)
                {
                    /*開LED 1*/
                    ledState_1 = true;
                }
                else
                {
                    /*關LED 1*/
                    ledState_1 = false;
                }
            }
            catch (Exception)
            {

            }
        }

        private void LED_2_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (!ledState_2)
                {
                    ledState_2 = true;
                }
                else
                {                    
                    ledState_2 = false;
                }
            }
            catch (Exception)
            {

            }
        }

        private void LED_3_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (!ledState_3)
                {
                    ledState_3 = true;
                }
                else
                {
                    ledState_3 = false;
                }
            }
            catch (Exception)
            {

            }
        }

        private void LED_4_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (!ledState_4)
                {
                    ledState_4 = true;
                }
                else
                {
                    ledState_4 = false;
                }
            }
            catch (Exception)
            {

            }
        }

        private void LED_5_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (!ledState_5)
                {
                    ledState_5 = true;
                }
                else
                {
                    ledState_5 = false;
                }
            }
            catch (Exception)
            {

            }
        }
    }
}
