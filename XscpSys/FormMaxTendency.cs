﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using XscpSys.Model;
using XscpSys.Controllers;
using System.Reflection;
using System.IO;

namespace XscpSys
{
    public partial class FormMaxTendency : Form
    {
        public int num1;//数字1
        public int num2;//数字2

        private int days = 0;
        private CoreMethod coreMethod = new CoreMethod();

        private List<Tendency2Model> maxTendencys = new List<Tendency2Model>();

        public FormMaxTendency(string text)
        {
            InitializeComponent();
            this.Text = text;
        }

        private void FormTendency_Load(object sender, EventArgs e)
        {
            this.dtpStart.Value = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        }

        private void initDgv1(List<Tendency2Model> lt, Tendency2Model maxTendency)
        {
            Tendency2Model tm;
            DgvController.AddRows(this.dgv1, lt.Count);
            for (int i = 0; i < lt.Count; i++)
            {
                tm = lt[i];
                this.dgv1[0, i].Value = i + 1;//局数
                this.dgv1[1, i].Value = tm.Dtime;//开奖日期

                this.dgv1[2, i].Value = tm.Big;//大
                this.dgv1[3, i].Value = tm.Small;//小
                this.dgv1[4, i].Value = tm.BigSmall;//大小
                this.dgv1[5, i].Value = tm.SmallBig;//小大

                this.dgv1[6, i].Value = "";

                this.dgv1[7, i].Value = tm.Odd;//奇
                this.dgv1[8, i].Value = tm.Pair;//偶
                this.dgv1[9, i].Value = tm.OddPair;//奇偶
                this.dgv1[10, i].Value = tm.PairOdd;//偶奇
                this.dgv1[11, i].Value = tm.Dbl;//重重
                this.dgv1[12, i].Value = tm.Sno;
            }

            refreshColor();
            refreshMaxTendency(maxTendency);
        }

        private void refreshMaxTendency(Tendency2Model tm)
        {
            int maxValue = 0;
            string strValue = "";
            for (int i = 0; i < this.dgv1.Rows.Count; i++)
            {
                for (int j = 2; j < this.dgv1.Columns.Count - 1; j++)
                {
                    strValue = this.dgv1[j, i].Value.ToString();
                    if (!string.IsNullOrEmpty(strValue))
                    {
                        maxValue = Convert.ToInt32(strValue);
                        switch (j)
                        {
                            case 2: if (maxValue == tm.Big) DgvController.SetDgvBackColorStyle(this.dgv1, i, j, Color.Yellow, 12); break;
                            case 3: if (maxValue == tm.Small) DgvController.SetDgvBackColorStyle(this.dgv1, i, j, Color.Yellow, 12); break;
                            case 4: if (maxValue == tm.BigSmall) DgvController.SetDgvBackColorStyle(this.dgv1, i, j, Color.Yellow, 12); break;
                            case 5: if (maxValue == tm.SmallBig) DgvController.SetDgvBackColorStyle(this.dgv1, i, j, Color.Yellow, 12); break;
                            case 7: if (maxValue == tm.Odd) DgvController.SetDgvBackColorStyle(this.dgv1, i, j, Color.Yellow, 12); break;
                            case 8: if (maxValue == tm.Pair) DgvController.SetDgvBackColorStyle(this.dgv1, i, j, Color.Yellow, 12); break;
                            case 9: if (maxValue == tm.OddPair) DgvController.SetDgvBackColorStyle(this.dgv1, i, j, Color.Yellow, 12); break;
                            case 10: if (maxValue == tm.PairOdd) DgvController.SetDgvBackColorStyle(this.dgv1, i, j, Color.Yellow, 12); break;
                            case 11: if (maxValue == tm.Dbl) DgvController.SetDgvBackColorStyle(this.dgv1, i, j, Color.Yellow, 12); break;
                            default: break;
                        }
                    }
                }
            }

        }

        private void refreshColor()
        {
            string week = "";
            for (int i = 0; i < this.dgv1.Rows.Count; i++)
            {
                week = this.dgv1[12, i].Value.ToString().ToLower();
                if (week == "saturday" || week == "sunday")
                {
                    this.dgv1.Rows[i].DefaultCellStyle.BackColor = Color.LightSteelBlue;
                }
            }

        }

        /// <summary>
        /// 获取最大走势值
        /// </summary>
        /// <returns></returns>
        private Tendency2Model GetMaxTendency(Tendency<Tendency2Model> tendency)
        {
            if (tendency.Lt_Tendencys.Count <= 0) return new Tendency2Model();
            Tendency2Model tm = new Tendency2Model();
            tm.Big = tendency.Lt_Tendencys.Max(l => l.Big);//大
            tm.Small = tendency.Lt_Tendencys.Max(l => l.Small);//小
            tm.BigSmall = tendency.Lt_Tendencys.Max(l => l.BigSmall);//大小
            tm.SmallBig = tendency.Lt_Tendencys.Max(l => l.SmallBig);//小大

            tm.Odd = tendency.Lt_Tendencys.Max(l => l.Odd);//奇
            tm.Pair = tendency.Lt_Tendencys.Max(l => l.Pair);//偶
            tm.OddPair = tendency.Lt_Tendencys.Max(l => l.OddPair);//奇偶
            tm.PairOdd = tendency.Lt_Tendencys.Max(l => l.PairOdd);//偶奇
            tm.Dbl = tendency.Lt_Tendencys.Max(l => l.Dbl);//重
            return tm;
        }

        /// <summary>
        /// 获取最大走势值
        /// </summary>
        /// <returns></returns>
        private Tendency2Model GetMaxTendency(List<Tendency2Model> Lt_Tendencys)
        {
            Tendency2Model tm = new Tendency2Model();
            tm.Big = Lt_Tendencys.Max(l => l.Big);//大
            tm.Small = Lt_Tendencys.Max(l => l.Small);//小
            tm.BigSmall = Lt_Tendencys.Max(l => l.BigSmall);//大小
            tm.SmallBig = Lt_Tendencys.Max(l => l.SmallBig);//小大

            tm.Odd = Lt_Tendencys.Max(l => l.Odd);//奇
            tm.Pair = Lt_Tendencys.Max(l => l.Pair);//偶
            tm.OddPair = Lt_Tendencys.Max(l => l.OddPair);//奇偶
            tm.PairOdd = Lt_Tendencys.Max(l => l.PairOdd);//偶奇
            tm.Dbl = Lt_Tendencys.Max(l => l.Dbl);//重
            return tm;
        }

        private void btnFind_Click(object sender, EventArgs e)
        {

            this.Cursor = Cursors.WaitCursor;

            TimeSpan d = this.dtpEnd.Value - this.dtpStart.Value;
            days = d.Days + 1;
            string fileName = null;

            Lottery lottery = new LotteryFF();
            Tendency<Tendency2Model> tendency = new Tendency<Tendency2Model>();
            Tendency2Model maxTendency = null;
            maxTendencys.Clear();
            for (int i = 0; i < days; i++)
            {
                fileName = Param.FileBase + @"分分彩\" + "ffcp" + this.dtpStart.Value.AddDays(i).ToString("yyyyMMdd") + ".txt";
                if (!File.Exists(fileName)) continue;
                coreMethod.ReadFile(this.dtpStart.Value.AddDays(i), lottery, fileName);//读取文件
                coreMethod.AnalyzeTendency(lottery, tendency, num1, num2);
                maxTendency = GetMaxTendency(tendency);
                maxTendency.Dtime = this.dtpStart.Value.AddDays(i).ToString("yyyyMMdd");
                maxTendency.Sno = this.dtpStart.Value.AddDays(i).DayOfWeek.ToString();
                maxTendencys.Add(maxTendency);
            }

            maxTendency = GetMaxTendency(maxTendencys);

            if (maxTendencys.Count > 0)
                initDgv1(maxTendencys, maxTendency);
            this.Cursor = null;
        }
    }
}
