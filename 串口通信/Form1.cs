using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;//为了获取com口的引用空间
using System.Collections;// arraylist的引用空间
using System.Threading;     //线程

//txt 文件
using System.IO;
using System.Text;


namespace 串口通信
{
    public partial class Form1 : Form
    {

        private int page = -1;
        MyserialPort classSerialPort = new MyserialPort();
        Thread workThread;
        static bool bFirstClickAutoRunBtn = false;
        static bool newStart = false;
        int testTimes = 0;
        int iGlobalOKCount = 0;
        int iGlobalNGCount = 0;
        int iGlobalerrorcode = 0;
        string GlobalReadData;
        string GlobalWriteData;
        static bool isReadyShowMessage = false;
        static bool bWantStop = false;
        int iTimeClick = 0;

        string strAbout;
        string temperatureStart;
        string temperatureEnd;

        public Form1()
        {
            InitializeComponent();
        }

        //初始化函数
        private void Form1_Load(object sender, EventArgs e)
        {

            string[] portName = SerialPort.GetPortNames();
            UpdateUI(1, portName);//显示第一个界面的UI

        }

        //具体的每一步实现
        public int MotionMove(string commandOne, string confirmOne, string commandTwo, string confirmTwo,int errorcode,bool isCheckSecond)
        {
            byte[] writeData = new byte[256];
            byte[] readData = new byte[256];
            bool res = true;

            int firstLoop = 2;
            /********************************************************************************************/
            if(true)
            {
                //1:write
                commandOne += "\r\n";
                writeData = new ASCIIEncoding().GetBytes(commandOne);
                int i = 0;
                for (i = 0; i < firstLoop; i++)
                {
                    res = classSerialPort.serialPortWriteData(0, writeData);
                    if (!res)
                    {
                        continue;
                        //return errorcode;
                    }
                    if (commandOne.Contains("setdraweropen") || commandOne.Contains("setdrawerclose"))
                    {
                        System.Threading.Thread.Sleep(2000);
                    }
                    else
                    {
                        System.Threading.Thread.Sleep(500);
                    }

                    //2:read
                    res = classSerialPort.serialPortReadData(0, 256, readData, confirmOne);
                    if (!res)
                    {
                        continue;
                        //return errorcode;
                    }
                    else
                    {
                        if (commandOne.Contains("about"))
                        {
                            strAbout = System.Text.Encoding.Default.GetString(readData);
                        }

                        if (commandTwo.Contains("start"))
                        {
                            temperatureStart = System.Text.Encoding.Default.GetString(readData);
                        }

                        if (commandTwo.Contains("end"))
                        {
                            temperatureEnd = System.Text.Encoding.Default.GetString(readData);
                        }
                        break;
                    }
                }
                if (i >= firstLoop && !res)
                {
                    return errorcode;
                }
            }


            //确定是不是需要发送状态的命令
            if (isCheckSecond)
            {
                /********************************************************************************************/
                //1:write sensor state
                commandTwo += "\r\n";
                writeData = new ASCIIEncoding().GetBytes(commandTwo);

                int i = 0;
                for (i = 0; i < firstLoop; i++)//test here
                {
                    res = classSerialPort.serialPortWriteData(0, writeData);
                    if (!res)
                    {
                        //return errorcode;
                        continue;
                    }

                    if (commandTwo.Contains("readdrawerposition"))
                    {
                        System.Threading.Thread.Sleep(2000);
                    }
                    else
                    {
                        System.Threading.Thread.Sleep(500);
                    }

                    //2:read sensor state
                    res = classSerialPort.serialPortReadData(0, 256, readData, confirmTwo);
                    if (!res)
                    {
                        //return errorcode;
                        continue;
                    }
                    else
                    {
                        break;
                    }
                }
                if (i >= firstLoop && !res)
                {
                    return errorcode;
                }
            }



            return 0;
        }

        /*
         string 转 byte[]
          byte[] byteArray = System.Text.Encoding.Default.GetBytes(str);
          
          byte[] 转string
          str =  System.Text.Encoding.Default.GetString(byteArray);
        */
        //控制函数
        public int MotionFunction()
        {
            byte[] writeData = new byte[256];
            byte[] readData = new byte[256];
            string strWriteBuf = "";//"voltage 3\r\n";
            bool res = true;

            //1:read serial number
            int errorcode = MotionMove("about", "Serial Number", "readcarrierlockposition", "_DOWN: ENGAGED", 3, false);
            if (errorcode != 0)
            {
                return errorcode;
            }

            //read temperature
            errorcode = MotionMove("readtemperature", "T0=", "start", "_DOWN: ENGAGED", 3, false);
            if (errorcode != 0)
            {
                return errorcode;
            }

            //inital motion
            //2:发送setcarrierlockdown
            errorcode = MotionMove("setcarrierlockdown", "OK", "readcarrierlockposition", "_DOWN: ENGAGED", 3, true);
            if (errorcode != 0)
            {
                return errorcode;
            }



            //1:发送:setmetermoveup
            errorcode = MotionMove("setmetermoveup", "OK", "readmeterposition", "METER_UP: ENGAGED", 2,true);
            if (errorcode != 0)
            {
                return errorcode;
            }

            iGlobalOKCount = 0;
            iGlobalNGCount = testTimes - iGlobalOKCount;
            //开始循环
            for(int i=1; i<=testTimes; i++)
            {
                if (bWantStop)
                {

                    //read temperature
                    errorcode = MotionMove("readtemperature", "T0=", "end", "_DOWN: ENGAGED", 3, false);
                    if (errorcode != 0)
                    {
                        return errorcode;
                    }
                    return 0;
                }

                //1:开门：setdraweropen
                errorcode = MotionMove("setdraweropen", "OK", "readdrawerposition", "Drawer: OPEN", 4, true);
                if (errorcode != 0)
                {
                    break;
                    //return errorcode;
                }


                /********************************************************************************************/
                //2:setcarrierlockup
                errorcode = MotionMove("setcarrierlockup", "OK", "readcarrierlockposition", "_UP: ENGAGED", 5,true);
                if (errorcode != 0)
                {
                    break;
                   // return errorcode;
                }

                /********************************************************************************************/
                //3:setcarrierlockdown
                errorcode = MotionMove("setcarrierlockdown", "OK", "readcarrierlockposition", "_DOWN: ENGAGED", 6, true);
                if (errorcode != 0)
                {
                    break;
                   // return errorcode;
                }


                /********************************************************************************************/
                //4:setmetermoveup
                errorcode = MotionMove("setmetermoveup", "OK", "readmeterposition", "METER_UP: ENGAGED", 7,true);
                if (errorcode != 0)
                {
                    break;
                    //return errorcode;
                }

                /********************************************************************************************/
                //5:setdrawerclose
                errorcode = MotionMove("setdrawerclose", "OK", "readdrawerposition", "Drawer: CLOSE", 8, true);
                if (errorcode != 0)
                {
                    break;
                    //return errorcode;
                }

                /********************************************************************************************/
                //6:setmetermovedown
                errorcode = MotionMove("setmetermovedown", "OK", "readmeterposition", "METER_DOWN: ENGAGED", 9,true);
                if (errorcode != 0)
                {
                    break;
                    //return errorcode;
                }

                /********************************************************************************************/
                //7:setbeamprofiler
                
                errorcode = MotionMove("setbeamprofiler", "OK", "readbeamprofilerposition", "ENGAGED", 10,true);
                if (errorcode != 0)
                {
                    break;
                    //return errorcode;
                }
               
                /********************************************************************************************/
                //8:setpowermeter
                
                errorcode = MotionMove("setpowermeter", "OK", "readpowermeterposition", "ENGAGED", 11,true);
                if (errorcode != 0)
                {
                    break;
                   // return errorcode;
                }
                

                /********************************************************************************************/
                //9:setbeamprofiler
              
                errorcode = MotionMove("setbeamprofiler", "OK", "readbeamprofilerposition", "ENGAGED", 12,true);
                if (errorcode != 0)
                {
                    break;
                    //return errorcode;
                }
                
                /********************************************************************************************/
                //10:setmetermoveup
                errorcode = MotionMove("setmetermoveup", "OK", "readmeterposition", "METER_UP: ENGAGED", 13,true);
                if (errorcode != 0)
                {
                    break;
                    //return errorcode;
                }


                /********************************************************************************************/
                //11:setfanon
                errorcode = MotionMove("setfanon", "OK", "readmeterposition", "METER_UP: ENGAGED", 14, false);
                if (errorcode != 0)
                {
                    break;
                    //return errorcode;
                }
                System.Threading.Thread.Sleep(200);
                //11:setfanoff
                errorcode = MotionMove("setfanoff", "OK", "readmeterposition", "METER_UP: ENGAGED", 15, false);
                if (errorcode != 0)
                {
                    break;
                    //return errorcode;
                }


                /********************************************************************************************/
                //12:setpassledon
                errorcode = MotionMove("setpassledon", "OK", "readmeterposition", "METER_UP: ENGAGED", 16, false);
                if (errorcode != 0)
                {
                    break;
                    //return errorcode;
                }
                System.Threading.Thread.Sleep(200);
                //13:setpassledoff
                errorcode = MotionMove("setpassledoff", "OK", "readmeterposition", "METER_UP: ENGAGED", 17, false);
                if (errorcode != 0)
                {
                    break;
                    //return errorcode;
                }


                /********************************************************************************************/
                //14:setfailledon
                errorcode = MotionMove("setfailledon", "OK", "readmeterposition", "METER_UP: ENGAGED", 17, false);
                if (errorcode != 0)
                {
                    break;
                    //return errorcode;
                }
                System.Threading.Thread.Sleep(200);
                //15:setfailledoff
                errorcode = MotionMove("setfailledoff", "OK", "readmeterposition", "METER_UP: ENGAGED", 18, false);
                if (errorcode != 0)
                {
                    break;
                    //return errorcode;
                }


                /********************************************************************************************/
                //16:setrunningledon
                errorcode = MotionMove("setrunningledon", "OK", "readmeterposition", "METER_UP: ENGAGED", 17, false);
                if (errorcode != 0)
                {
                    break;
                    //return errorcode;
                }
                System.Threading.Thread.Sleep(200);
                //17:setrunningledoff
                errorcode = MotionMove("setrunningledoff", "OK", "readmeterposition", "METER_UP: ENGAGED", 18, false);
                if (errorcode != 0)
                {
                    break;
                    //return errorcode;
                }

                /********************************************************************************************/
                iGlobalOKCount = i;
                iGlobalNGCount = testTimes - iGlobalOKCount;
            }

            if(errorcode != 0)
            {
                System.Threading.Thread.Sleep(5000);
            }

            //read temperature
            int readTemperature = MotionMove("readtemperature", "T0=", "end", "_DOWN: ENGAGED", 3, false);
            if (readTemperature != 0)
            {
                return readTemperature;
            }

            if (errorcode != 0)
            {
                return errorcode;
            }
            return 0;
        }


        //线程的处理函数
        void WorkThreadFunction()
        {
            while (true)
            {
                if (newStart == false)
                {
                    System.Threading.Thread.Sleep(1000);
                    continue;
                }

                //添加循环次数
                iGlobalerrorcode = MotionFunction();
                newStart = false;


                System.Threading.Thread.Sleep(3000);
                //测试完成写结束的文件

                string fileLogName;
                int iStart = strAbout.IndexOf("Serial Number:");
                iStart += 14;
                fileLogName = strAbout.Substring(iStart, 5);
                string totalFileLogName = "D:\\" + fileLogName + ".txt";

                //FileStream fs = new FileStream("D:\\BurntestDiagnostics.txt", FileMode.Create);
                FileStream fs = new FileStream(totalFileLogName, FileMode.Create);
                //开始写入
                string toatlStr = "";
                int index = 0;
                index = temperatureStart.IndexOf('=');
                temperatureStart = temperatureStart.Substring(index + 1,5);

                index = temperatureEnd.IndexOf('=');
                temperatureEnd = temperatureEnd.Substring(index + 1,5);

                toatlStr = toatlStr + "start temperature\n" + temperatureStart + "\r\n";
                toatlStr = toatlStr + "end temperature\n" + temperatureEnd + "\r\n";

                toatlStr = toatlStr + "TotalLoop:" + textBoxTimesOne.Text + "\r\n";

                toatlStr = toatlStr + "Completed:" + iGlobalOKCount.ToString() + "\r\n";

                toatlStr = toatlStr + "Test time:" + iTimeClick.ToString() + "seconds" + "\r\n";

                toatlStr = toatlStr + "State:" + textBoxErrorOne.Text + "\r\n";

                DateTime dt = DateTime.Now;
                toatlStr = toatlStr + "\r\n" + "test recorded: " + dt.ToString() + "\r\n";

                toatlStr += strAbout;

                byte[] totalData = System.Text.Encoding.Default.GetBytes(toatlStr);
                fs.Write(totalData, 0, toatlStr.Length);
                //清空缓冲区、关闭流
                fs.Flush();
                fs.Close();

            }
        }

        //第一个串口设置
        private void buttonOne_Click(object sender, EventArgs e)
        {
            buttonOne.Enabled = false;
            strAbout = "";
            temperatureStart = "";
            temperatureEnd = "";
            bWantStop = false;

            //1:获取测试次数
            string strTestTimes = textBoxTimesOne.Text;
            if (strTestTimes.Length < 1)
            {
                strTestTimes = "1";
                textBoxTimesOne.Text = "1";
            }
            testTimes = Convert.ToInt32(strTestTimes);
            iTimeClick = 0;
            ShowErrorMessage(1, 31);
            iGlobalerrorcode = 31;
            if (bFirstClickAutoRunBtn == false)
            {
                Tonytimer.Start();
                Hanoitimer.Start();
                workThread = new Thread(new ThreadStart(WorkThreadFunction));
                workThread.Start();
                bFirstClickAutoRunBtn = true;
            }

            if (newStart == false)
            {
                System.Threading.Thread.Sleep(500);//ensure to enter the thread
                newStart = true;
            }  

        }
        //第一个串口连接到设备
        private void buttonConnectOne_Click(object sender, EventArgs e)
        {
            GetConfig(1);//代表第一个界面
        }

        //第一个串口导入命令
        private void buttonImportOne_Click(object sender, EventArgs e)
        {
            page = 1;
            ArrayList commandArray = new ArrayList();
            ArrayList sleepTimeArray = new ArrayList();
            ParseCommand(ref commandArray, ref sleepTimeArray, page);
            ShowCommand(commandArray, sleepTimeArray, page);
 

        }



        /*********************************************************************************************/
        //解析命令
        /*********************************************************************************************/
        private void ParseCommand(ref ArrayList commandArray, ref ArrayList sleepTimeArray, int page)
        {
            string strTotal = "";
            switch(page)
            {
                case 1:
                    strTotal = textBoxCommandOne.Text;
                    break;
            }

            string[] strLine = strTotal.Split('\n');

            string strCommand = "";
            string strSleep = "";
            int index = 0;
            int sleepTime = 0;

            
            foreach(string str in strLine)
            {
                string tempStr = str.Trim();//去掉所有的空格
                index = str.IndexOf(',');
                if (index < 0)
                {
                    continue;
                }
                strCommand = str.Substring(0, index) + "\r\n";
                strSleep = str.Substring(index + 1,(str.Length-index-2));//-2是为了去掉回车符
                if (strSleep.Length < 16 )
                {
                    bool isDigital = true;
                    foreach(char ch in strSleep)
                    {
                        if(ch < '0' || ch > '9')
                        {
                            isDigital = false;
                            break;
                        }
                    }
                    if (isDigital)
                    {
                        sleepTime = Convert.ToInt32(strSleep);
                        commandArray.Add(strCommand);
                        sleepTimeArray.Add(sleepTime);
                    }
                }
            }
        }

        /*********************************************************************************************/
        //显示命令
        /*********************************************************************************************/
        private void ShowCommand(ArrayList commandArray, ArrayList sleepTimeArray, int page)
        {
            TextBox show = null;
            switch (page)
            {
                case 1:
                    show = textBoxMessageOne;
                    break;
            }

            //在界面显示给用户确认
            show.Text = "请确认你需要执行的命令:\n";
            string strMessage = "";
            for (int i = 0; i < commandArray.Count; i++)
            {
                //MessageBox.Show(commandArray[i].ToString());
                strMessage = "\n命令" + (i + 1).ToString() + ":";
                show.AppendText(strMessage);
                show.AppendText(commandArray[i].ToString());

                strMessage = "\n延迟时间" + ":";
                show.AppendText(strMessage);
                show.AppendText(sleepTimeArray[i].ToString() + "\n" + "\n");

            }
        }

        /*********************************************************************************************/
        //获取COM的配置
        /*********************************************************************************************/
        private bool GetConfig(int page)
        {
            //1:get the com port
            ComboBox tempCom = null;
            ComboBox tempBaudRate = null;
            ComboBox tempByteSize = null;
            ComboBox tempParity = null;
            ComboBox tempStopBit = null;
            ComboBox tempControl = null;

            string strCom = "";
            string strBaudRate = "";
            string strByteSize = "";
            string strParity = "";
            string strStopBits = "";
            string strControl = "";
            switch (page)
            {
                case 1:
                    tempCom = comboBoxCOM;
                    tempBaudRate = comboBoxBaudRate;
                    tempByteSize = comboBoxByteSize;
                    tempParity = comboBoxParity;
                    tempStopBit = comboBoxStopBit;
                    tempControl = comboBoxControl;
                    break;
            }
            //1:get the com port
            strCom = tempCom.SelectedItem.ToString();

            //2:获取波特率
            strBaudRate = tempBaudRate.SelectedItem.ToString();

            //3:获取数据位
            strByteSize = tempByteSize.SelectedItem.ToString();

            //4:获取校验位
            strParity = tempParity.SelectedItem.ToString();

            //5:获取停止位
            strStopBits = tempStopBit.SelectedItem.ToString();

            //6:获取流控
            strControl = tempControl.SelectedItem.ToString();

            //开始转化
            int iBaudRate = Convert.ToInt32(strBaudRate); //波特率
            byte iByteBits = Convert.ToByte(strByteSize);
            byte iStopBits = 0;                              //停止位
            byte iParity = 0;         
            switch(strStopBits)                          
            {
                case "1":
                    iStopBits = 1;//
                    break;
                case "1.5":
                    iStopBits = 3;//StopBits.One;
                    break;
                case "2":
                    iStopBits = 2;
                    break;
            }
            switch (strParity)
            {
                case "None":
                    iParity = 0;//Parity.None;
                    break;
                case "Even":
                    iParity = 1;
                    break;
                case "Odd":
                    iParity = 2;
                    break;
                case "Mark":
                    iParity = 3;
                    break;
                case "Space":
                    iParity = 4;
                    break;
            }
  
            bool res = classSerialPort.serialPortOpenCom((page - 1), strCom, iBaudRate, iParity, iByteBits, iStopBits);
            if(!res)
            {
                ShowErrorMessage(page, 1);
            }
            else
            {
                ShowErrorMessage(page, 30);
                iGlobalerrorcode = 30;
            }
            
            return true;
        }

        /*********************************************************************************************/
        //更新UI
        /*********************************************************************************************/
        private void UpdateUI(int page, string[] portName)
        {

            ComboBox tempCom = null;
            ComboBox tempBaudRate = null;
            ComboBox tempByteSize = null;
            ComboBox tempParity = null;
            ComboBox tempStopBit = null;
            ComboBox tempControl = null;
            TextBox temptextBoxCommandOne = null;
            TextBox temptextBoxDebugOne = null;
            TextBox temptextBoxMessageOne = null;

            switch (page)
            {
                case 1:
                    tempCom = comboBoxCOM;
                    tempBaudRate = comboBoxBaudRate;
                    tempByteSize = comboBoxByteSize;
                    tempParity = comboBoxParity;
                    tempStopBit = comboBoxStopBit;
                    tempControl = comboBoxControl;
                    temptextBoxCommandOne = textBoxCommandOne;
                    temptextBoxDebugOne = textBoxDebugOne;
                    temptextBoxMessageOne = textBoxMessageOne;
                    break;
            }
            /**************************************************************************************/
            //com口设置
            foreach (string str in portName)
            {
                comboBoxCOM.Items.Add(str);
            }
            tempCom.SelectedIndex = 0;

            //波特率设置
            tempBaudRate.Items.Add("9600");
            tempBaudRate.Items.Add("19200");
            tempBaudRate.Items.Add("38400");
            tempBaudRate.Items.Add("57600");
            tempBaudRate.Items.Add("115200");
            tempBaudRate.SelectedIndex = 4;

            //数据位设置
            tempByteSize.Items.Add("5");
            tempByteSize.Items.Add("6");
            tempByteSize.Items.Add("7");
            tempByteSize.Items.Add("8");
            comboBoxByteSize.SelectedIndex = 3;

            //校验位
            tempParity.Items.Add("none");
            tempParity.Items.Add("Even");
            tempParity.Items.Add("0dd");
            tempParity.Items.Add("Mark");
            tempParity.Items.Add("Spaces");
            tempParity.SelectedIndex = 0;

            //停止位
            tempStopBit.Items.Add("1");
            tempStopBit.Items.Add("1.5");
            tempStopBit.Items.Add("2");
            tempStopBit.SelectedIndex = 0;

            //流控
            tempControl.Items.Add("None");
            tempControl.Items.Add("RTS/CTS");
            tempControl.Items.Add("XON/XOFF");
            tempControl.SelectedIndex = 0;

            //1:实现滚动条
            temptextBoxCommandOne.ScrollBars = ScrollBars.Vertical;
            temptextBoxDebugOne.ScrollBars = ScrollBars.Vertical;
            temptextBoxMessageOne.ScrollBars = ScrollBars.Vertical;
            /**************************************************************************************/
        }

        
        /*********************************************************************************************/
        //清除第一个的消息框
        /*********************************************************************************************/
        private void buttonClear_Click(object sender, EventArgs e)
        {
            textBoxMessageOne.Text = "";
        }

        /*********************************************************************************************/
        //显示error message
        /*********************************************************************************************/
        public void ShowErrorMessage(int page,int errorcode)
        {
            TextBox temptextBoxError = null;
            switch (page)
            {
                case 1:
                    temptextBoxError = textBoxErrorOne;
                    break;
            }

            string message = "";
            switch(errorcode)
            {
                case 0:
                    message = "Test Finish";
                    break;
                case 1:
                    message = "连接COM口失败";
                    break;
                case 2:
                    message = "setmetermoveup fail 2";
                    break;
                case 3:
                    message = "setcarrierlockdown fail 3";
                    break;
                case 4:
                    message = "setdraweropen fail 4";
                    break;
                case 5:
                    message = "setcarrierlockup 5";
                    break;
                case 6:
                    message = "setcarrierlockdown 6";
                    break;
                case 7:
                    message = "setmetermoveup fail 7";
                    break;
                case 8:
                    message = "setdrawerclose fail 8";
                    break;
                case 9:
                    message = "setmetermovedown fail 9";
                    break;
                case 10:
                    message = "setbeamprofiler fail 10";
                    break;
                case 11:
                    message = "setpowermeter fail 11";
                    break;
                case 12:
                    message = "setbeamprofiler fail 12";
                    break;
                case 13:
                    message = "setmetermoveup fail 13";
                    break;
                case 30:
                    message = "连接设备成功";
                    break;
                case 31:
                    message = "我正在测试，请不要打扰";
                    break;

            }
            temptextBoxError.Text = message;
            //textBoxErrorOne.Text = message;
            if (errorcode == 0)
            {
                temptextBoxError.BackColor = System.Drawing.Color.Green;
            }
            else if (errorcode == 30 || errorcode == 31)
            {
                temptextBoxError.BackColor = System.Drawing.Color.Orange;
            }
            else
            {
                temptextBoxError.BackColor = System.Drawing.Color.Red;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            System.IO.Ports.SerialPort a = new SerialPort();
            a.PortName = comboBoxCOM.Text;
            a.Open();
            a.BaudRate = Convert.ToInt32(comboBoxBaudRate.Text);
            a.DataBits = 8;
            a.Parity = Parity.None;
            a.StopBits = StopBits.One;
            a.WriteLine("voltage 1\r\n");
            
        }

        /*****************************************************************/
        //关闭整个程序
        /*****************************************************************/
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            classSerialPort.serialPortClosePort(0);
            Tonytimer.Stop();
        }

        //这里实时更新ok 和 NG的数
        private void Tonytimer_Tick(object sender, EventArgs e)
        {
            Tonytimer.Stop();

            //iGlobalOKCount += 1;
            //iGlobalNGCount -= 1;
            if (newStart == false)
            {
                buttonOne.Enabled = true;
            }
            OKOne.Text = iGlobalOKCount.ToString();
            NGOne.Text = iGlobalNGCount.ToString();
            ShowErrorMessage(1, iGlobalerrorcode);
            Tonytimer.Start();
        }


        //显示每次发送的信息
        private void Hanoitimer_Tick(object sender, EventArgs e)
        {

            Hanoitimer.Stop();
            string strTime = iTimeClick.ToString();
            textBoxMessageOne.Text = strTime;


            if (iGlobalerrorcode != 0 && iGlobalerrorcode != 31)
            {
                textBoxMessageOne.Text = "ERROR";
                return;
            }
            
            int temp = iTimeClick % 8;
            if(temp == 0)
            {
                TonyShowMessage.Text = "珠海市博杰电子科技有限公司";
            }
            else if(temp == 1)
            {
                TonyShowMessage.Text = "ZhuHai Bojay Electronics co.,LTD";
            }
            else if(temp == 2)
            {
                TonyShowMessage.Text = "隔音箱";
            }
            else if (temp == 3)
            {
                TonyShowMessage.Text = "屏蔽箱";
            }
            else if (temp == 4)
            {
                TonyShowMessage.Text = "ICT";
            }
            else if (temp == 5)
            {
                TonyShowMessage.Text = "自动化设备";
            }
            else if (temp == 6)
            {
                TonyShowMessage.Text = "LED Calibration";
            }
            else if (temp == 7)
            {
                TonyShowMessage.Text = "光谱仪Spectrometer";
            }
            iTimeClick++;
            Hanoitimer.Start();
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {

            Tonytimer.Stop();
            Hanoitimer.Stop();
            textBoxMessageOne.Text = "You stoped,What do you do?";

            bWantStop = true;
            return;
            if(strAbout.Length > 0 && temperatureStart.Length > 0 && temperatureEnd.Length > 0)
            {

                /*
                //测试完成写结束的文件
                FileStream fs = new FileStream("D:\\BurntestDiagnostics.txt", FileMode.Create);
                //开始写入
                string toatlStr = "";
                int index = 0;
                index = temperatureStart.IndexOf('=');
                temperatureStart = temperatureStart.Substring(index + 1, 5);

                index = temperatureEnd.IndexOf('=');
                temperatureEnd = temperatureEnd.Substring(index + 1, 5);

                toatlStr = toatlStr + "start temperature\n" + temperatureStart + "\r\n";
                toatlStr = toatlStr + "end temperature\n" + temperatureEnd + "\r\n";

                toatlStr = toatlStr + "TotalLoop:" + textBoxTimesOne.Text + "\r\n";

                toatlStr = toatlStr + "Completed:" + iGlobalOKCount.ToString() + "\r\n";

                toatlStr = toatlStr + "Test time:" + iTimeClick.ToString() + "seconds" + "\r\n";

                DateTime dt = DateTime.Now;
                toatlStr = toatlStr + "\r\n" + "test recorded: " + dt.ToString() + "\r\n";

                toatlStr += strAbout;

                byte[] totalData = System.Text.Encoding.Default.GetBytes(toatlStr);
                fs.Write(totalData, 0, toatlStr.Length);
                //清空缓冲区、关闭流
                fs.Flush();
                fs.Close();
                 * */
            }
        }

        private void buttonContiune_Click(object sender, EventArgs e)
        {
            /*
            //1:read serial number
            int errorcode = MotionMove("about", "Serial Number", "readcarrierlockposition", "_DOWN: ENGAGED", 3, false);
            if (errorcode != 0)
            {
                return;
            }

            string fileLogName;
            int iStart = strAbout.IndexOf("Serial Number:");
            iStart += 14;
            fileLogName = strAbout.Substring(iStart, 5);
            string totalFileLogName = "D:\\" + fileLogName + ".txt";

            //FileStream fs = new FileStream("D:\\BurntestDiagnostics.txt", FileMode.Create);
            FileStream fs = new FileStream(totalFileLogName, FileMode.Create);
             * */
        }
    }
}
