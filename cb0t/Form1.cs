﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Threading;

namespace cb0t
{
    public partial class Form1 : Form
    {
        private ChannelBar channel_bar { get; set; }
        
        private SettingsPanel settings_content { get; set; }
        private AudioPanel audio_content { get; set; }
        private ChannelListPanel clist_content { get; set; }
        private Thread sock_thread { get; set; }

        public Form1()
        {
            Settings.Create();
            Emoticons.Load();

            this.InitializeComponent();
            this.toolStrip1.Items.Add(new SettingsButton());
            this.toolStrip1.Items.Add(new ToolStripSeparator());
            this.toolStrip1.Items.Add(new AudioButton());
            this.toolStrip1.Items.Add(new ToolStripSeparator());
            this.toolStrip1.Items.Add(new ChannelListButton());
            this.channel_bar = new ChannelBar();
            this.toolStrip1.Renderer = this.channel_bar;

            this.settings_content = new SettingsPanel();
            this.settings_content.BackColor = Color.White;
            this.settings_content.Dock = DockStyle.Fill;
            this.settings_content.Font = new Font("Tahoma", 8.25F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            this.audio_content = new AudioPanel();
            this.audio_content.BackColor = Color.White;
            this.audio_content.Dock = DockStyle.Fill;
            this.audio_content.Font = new Font("Tahoma", 8.25F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            this.clist_content = new ChannelListPanel();
            this.clist_content.BackColor = Color.WhiteSmoke;
            this.clist_content.Dock = DockStyle.Fill;
            this.clist_content.Font = new Font("Tahoma", 8.25F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            this.SetToList();

            Aero.HideIconAndText(this);
        }

        private void toolStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            if (e.ClickedItem is SettingsButton)
            {
                this.channel_bar.Mode = ChannelBar.ModeOption.Settings;
                this.SetToSettings();
            }
            else if (e.ClickedItem is AudioButton)
            {
                this.channel_bar.Mode = ChannelBar.ModeOption.Audio;
                this.SetToAudio();
            }
            else if (e.ClickedItem is ChannelListButton)
            {
                this.channel_bar.Mode = ChannelBar.ModeOption.ChannelList;
                this.SetToList();
            }
            else if (e.ClickedItem is ChannelButton)
            {
                this.channel_bar.Mode = ChannelBar.ModeOption.Channel;
                this.channel_bar.SelectedButton = ((ChannelButton)e.ClickedItem).EndPoint;
                this.SetToRoom(((ChannelButton)e.ClickedItem).EndPoint);
            }
            else return;

            this.toolStrip1.Invalidate();
        }

        private void SetToList()
        {
            while (this.content1.Controls.Count > 0)
                this.content1.Controls.RemoveAt(0);

            this.content1.Controls.Add(this.clist_content);
        }

        private void SetToSettings()
        {
            while (this.content1.Controls.Count > 0)
                this.content1.Controls.RemoveAt(0);

            this.content1.Controls.Add(this.settings_content);
        }

        private void SetToAudio()
        {
            while (this.content1.Controls.Count > 0)
                this.content1.Controls.RemoveAt(0);

            this.content1.Controls.Add(this.audio_content);
        }

        private void SetToRoom(IPEndPoint ep)
        {
            while (this.content1.Controls.Count > 0)
                this.content1.Controls.RemoveAt(0);

            foreach (Room room in RoomPool.Rooms)
                if (room.EndPoint.Equals(ep))
                {
                    this.content1.Controls.Add(room.Panel);
                    break;
                }
        }

        private void toolStrip1_Resize(object sender, EventArgs e)
        {
            Aero.ExtendTop(this, this.toolStrip1.Height);
        }

        private bool do_once = false;
        private bool terminate = false;

        private void Form1_Load(object sender, EventArgs e)
        {
            if (!this.do_once)
            {
                this.sock_thread = new Thread(new ThreadStart(this.SocketThread));
                this.sock_thread.Start();

                int frm_size_x = Settings.GetReg<int>("form_x", -1);
                int frm_size_y = Settings.GetReg<int>("form_y", -1);

                if (frm_size_x > 0 && frm_size_y > 0)
                    this.ClientSize = new Size(frm_size_x, frm_size_y);

                this.do_once = true;
                Aero.ExtendTop(this, this.toolStrip1.Height);
                Settings.CAN_WRITE_REG = false;
                this.clist_content.LabelChanged += this.ChannelListLabelChanged;
                this.clist_content.OpenChannel += this.OpenChannel;
                this.clist_content.Create();

                foreach (FavouritesListItem f in this.clist_content.GetAutoJoinRooms())
                    this.OpenChannel(null, new OpenChannelEventArgs { Room = f });

                Settings.CAN_WRITE_REG = true;
            }
        }

        private void SocketThread()
        {
            while (true)
            {
                if (this.terminate)
                    return;

                Room[] pool = RoomPool.Rooms.ToArray();

                for (int i = 0; i < pool.Length; i++)
                {

                }

                Thread.Sleep(30);
            }
        }

        private void OpenChannel(object sender, OpenChannelEventArgs e)
        {
            IPEndPoint ep = new IPEndPoint(e.Room.IP, e.Room.Port);
            int index = RoomPool.Rooms.FindIndex(x => x.EndPoint.Equals(ep));

            if (index > -1)
            {
                this.SetToRoom(RoomPool.Rooms[index].EndPoint);
                this.channel_bar.Mode = ChannelBar.ModeOption.Channel;
                this.channel_bar.SelectedButton = RoomPool.Rooms[index].EndPoint;
                this.toolStrip1.Invalidate();
            }
            else
            {
                Room room = new Room
                {
                    EndPoint = new IPEndPoint(e.Room.IP, e.Room.Port),
                    Credentials = e.Room
                };

                room.Button = new ChannelButton(room.Credentials);
                room.Panel = new RoomPanel();
                RoomPool.Rooms.Add(room);
                this.toolStrip1.Items.Add(room.Button);
                this.SetToRoom(room.EndPoint);
                this.channel_bar.Mode = ChannelBar.ModeOption.Channel;
                this.channel_bar.SelectedButton = room.EndPoint;
                this.toolStrip1.Invalidate();
            }
        }

        private void ChannelListLabelChanged(object sender, ChannelListLabelChangedEventArgs e)
        {
            this.toolStrip1.BeginInvoke((Action)(() =>
            {
                for (int i = 0; i < this.toolStrip1.Items.Count; i++)
                    if (this.toolStrip1.Items[i] is ChannelListButton)
                    {
                        this.toolStrip1.Items[i].Text = e.Text;
                        break;
                    }
            }));
        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {
            if (this.channel_bar.Mode != ChannelBar.ModeOption.Channel)
                e.Cancel = true;
            else
            {
                // move left/right
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.terminate = true;
            this.clist_content.Terminate = true;
        }

        private void Form1_ResizeEnd(object sender, EventArgs e)
        {
            if (this.do_once)
            {
                int frm_size_x = Settings.GetReg<int>("form_x", -1);
                int frm_size_y = Settings.GetReg<int>("form_y", -1);

                if (this.ClientSize.Width != frm_size_x || this.ClientSize.Height != frm_size_y)
                {
                    Settings.SetReg("form_x", this.ClientSize.Width);
                    Settings.SetReg("form_y", this.ClientSize.Height);
                }
            }
        }


    }
}