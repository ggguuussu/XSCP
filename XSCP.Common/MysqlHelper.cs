﻿using System;
using System.Collections.Generic;
using System.Data.SQLite;
using Dapper;
using System.IO;
using System.Linq;
using XSCP.Common.Model;
using MySteel.Common.Helper;
using MySql.Data.MySqlClient;

namespace XSCP.Common
{
    public class MysqlHelper
    {
        private static string mysqlConnectString = AppSettingsHelper.GetStringValue("MysqlConnectString");

        private static string DbName = AppSettingsHelper.GetStringValue("DbName");

        /// <summary>
        /// Google Cookie存储路径
        /// </summary>
        public static string CookiePath = null;

        /// <summary>
        /// 开奖记录
        /// </summary>
        public static string LottoryTbName = "Lottery_" + DateTime.Now.Year;

        /// <summary>
        /// 一星走势
        /// </summary>
        public static string TendencyDigit1TbName = "TendencyDigit1_" + DateTime.Now.Year;   //万
        public static string TendencyDigit2TbName = "TendencyDigit2_" + DateTime.Now.Year;   //千
        public static string TendencyDigit3TbName = "TendencyDigit3_" + DateTime.Now.Year;   //百
        public static string TendencyDigit4TbName = "TendencyDigit4_" + DateTime.Now.Year;   //十
        public static string TendencyDigit5TbName = "TendencyDigit5_" + DateTime.Now.Year;   //个

        ///前二后二包胆趋势
        public static string Tendency1TbName = "Tendency1_" + DateTime.Now.Year;

        /// <summary>
        /// 所有数字趋势
        /// </summary>
        public static string TendencyAllTbName = "TendencyAll_" + DateTime.Now.Year;

        /// <summary>
        /// 二星走势
        /// </summary>
        public static string TendencyBefore2TbName = "TendencyBefore2_" + DateTime.Now.Year;
        public static string TendencyAfter2TbName = "TendencyAfter2_" + DateTime.Now.Year;

        static MysqlHelper()
        {
            //@2.建表
            CreateLotteryTable(LottoryTbName);

            ///创建一星趋势表
            CreateTendencyDigit1Table(TendencyDigit1TbName);   //万
            CreateTendencyDigit1Table(TendencyDigit2TbName);   //千
            CreateTendencyDigit1Table(TendencyDigit3TbName);   //百
            CreateTendencyDigit1Table(TendencyDigit4TbName);   //十
            CreateTendencyDigit1Table(TendencyDigit5TbName);   //个

            ///前二后二包胆趋势
            CreateTendency1Table(Tendency1TbName);   //单个数字

            ///所有数字
            CreateAllTendency1Table(TendencyAllTbName);

            ///创建二星趋势表
            CreateTendency2Table(TendencyBefore2TbName);
            CreateTendency2Table(TendencyAfter2TbName);
        }

        /// <summary>
        /// 创建连接
        /// </summary>
        /// <returns></returns>
        public static MySqlConnection CreateConnection()
        {
            var connection = new MySqlConnection(mysqlConnectString);
            //connection.Open();
            return connection;
        }

        /// <summary>
        /// 创建索引
        /// </summary>
        /// <param name="tableName"></param>
        private static void CreateIndex(string tableName)
        {
            string sql = string.Format("CREATE UNIQUE INDEX {1}_index  USING BTREE ON {1} (ymd,sno)", tableName, tableName);
            using (MySqlConnection conn = CreateConnection())
            {
                conn.Execute(sql);
            }
        }

        #region 清除重复数据
        /// <summary>
        ///  清除重复数据
        /// </summary>
        public static void ClearRepeatData()
        {
            ClearRepeatData(LottoryTbName);

            ///创建一星趋势表
            ClearRepeatData(TendencyDigit1TbName);   //万
            ClearRepeatData(TendencyDigit2TbName);   //千
            ClearRepeatData(TendencyDigit3TbName);   //百
            ClearRepeatData(TendencyDigit4TbName);   //十
            ClearRepeatData(TendencyDigit5TbName);   //个

            ///前二后二包胆趋势
            ClearRepeatData(Tendency1TbName);   //单个数字

            ///所有数字
            ClearRepeatData(TendencyAllTbName);

            ///创建二星趋势表
            ClearRepeatData(TendencyBefore2TbName);
            ClearRepeatData(TendencyAfter2TbName);
        }

        /// <summary>
        /// 清除重复数据
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public static void ClearRepeatData(string tableName)
        {
            List<TendencyModel> lt = null;
            string sql = string.Format("SELECT ymd,sno from {0} group by ymd,sno having count(1)>1", tableName);
            using (MySqlConnection conn = CreateConnection())
            {
                lt = conn.Query<TendencyModel>(sql).ToList();
            }

            if (lt != null && lt.Count > 0)
            {
                lt.ForEach(item =>
                {
                    List<int> lt_int = new List<int>();
                    sql = string.Format("SELECT id from {0} where ymd='{1}' and sno ='{2}'", tableName, item.Ymd, item.Sno);
                    using (MySqlConnection conn = CreateConnection())
                    {
                        lt_int = conn.Query<int>(sql).ToList();
                    }

                    if (lt_int != null && lt_int.Count > 0)
                    {
                        for (int i = 1; i < lt_int.Count; i++)
                        {
                            sql = string.Format("delete from {0} where id ={1}", tableName, lt_int[i]);
                            using (MySqlConnection conn = CreateConnection())
                            {
                                conn.Execute(sql);
                            }
                        }
                    }
                });
            }
        }
        #endregion

        #region [开奖号码]
        /// <summary>
        /// 创建奖号数据表
        /// </summary>
        /// <param name="conn"></param>
        public static void CreateLotteryTable(string tableName)
        {
            string sql = string.Format("SELECT count(1) FROM information_schema.TABLES WHERE TABLE_SCHEMA ='{0}' and table_name ='{1}'", DbName, tableName);

            using (MySqlConnection conn = CreateConnection())
            {

                int count = conn.Query<int>(sql).FirstOrDefault();
                if (count == 0)
                {
                    sql = string.Format("CREATE TABLE {0} ( " +
                                   @"ID      int  auto_increment not null primary key,     " +
                                   @"Ymd     CHAR( 8 )      NOT NULL,	     " +
                                   @"Sno     CHAR( 4 )      NOT NULL,	     " +
                                   @"Lottery CHAR( 9 )      NOT NULL,	     " +
                                   @"Num1    int             NOT NULL,	     " +
                                   @"Num2    int             NOT NULL,	     " +
                                   @"Num3    int             NOT NULL,	     " +
                                   @"Num4    int             NOT NULL,	     " +
                                   @"Num5    int             NOT NULL,	     " +
                                   @"Dtime   CHAR( 12 )      NOT NULL 	     )", tableName);
                    conn.Execute(sql);

                    CreateIndex(tableName);
                }
            }
        }

        /// <summary>
        /// 新增开奖数据
        /// </summary>
        /// <param name="lotterys"></param>
        public static void SaveLotteryData(List<LotteryModel> lotterys)
        {
            using (MySqlConnection conn = CreateConnection())
            {
                conn.Open();
                MySqlTransaction trans = conn.BeginTransaction();

                string sqlCount = string.Format("SELECT count(1) FROM {0} where Ymd = @Ymd and Sno=@Sno", LottoryTbName);

                try
                {
                    for (int i = lotterys.Count - 1; i >= 0; i--)
                    {
                        LotteryModel lm = lotterys[i];
                        int count = conn.Query<int>(sqlCount, lm).FirstOrDefault();
                        if (count == 0)
                        {
                            string sql = string.Format("insert into {0}(Ymd,Sno,Lottery,Num1,Num2,Num3,Num4,Num5,Dtime) VALUES( @Ymd,@Sno,@Lottery,@Num1,@Num2,@Num3,@Num4,@Num5,@Dtime)", LottoryTbName);
                            conn.Execute(sql, lm, trans);
                        }
                    }
                    trans.Commit();
                }
                catch (Exception)
                {
                    trans.Rollback();
                }
            }
        }

        /// <summary>
        /// 通过日期查找开奖号码
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static List<LotteryModel> QueryLottery(string date)
        {
            using (MySqlConnection conn = CreateConnection())
            {
                string sql = string.Format("select * from {0} where Ymd = '{1}' order by Sno desc", LottoryTbName, date);
                var lt = conn.Query<LotteryModel>(sql).ToList();
                return lt;
            }
        }

        /// <summary>
        /// 查找指定日期期号
        /// </summary>
        /// <param name="date"></param>
        /// <param name="sno"></param>
        /// <returns></returns>
        public static LotteryModel QueryLottery(string date, string sno)
        {
            using (MySqlConnection conn = CreateConnection())
            {
                string sql = string.Format("select * from {0} where Ymd = '{1}' and sno ='{2}'", LottoryTbName, date, sno);
                return conn.Query<LotteryModel>(sql).FirstOrDefault();
            }
        }

        /// <summary>
        /// 通过日期查找最近开奖期数
        /// </summary>
        /// <param name="date">日期</param>
        /// <param name="topNum">最近期数</param>
        /// <returns></returns>
        public static List<LotteryModel> QueryLottery(string date, int topNum)
        {
            using (MySqlConnection conn = CreateConnection())
            {
                string sql = string.Format("select * from {0} where Ymd = '{1}' order by Sno desc limit 0,{2}", LottoryTbName, date, topNum);
                var lt = conn.Query<LotteryModel>(sql).ToList();
                return lt;
            }
        }

        /// <summary>
        /// 检测该日期下缺失的期数
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static List<int> CheckLottery(string date)
        {
            using (MySqlConnection conn = CreateConnection())
            {
                string sql = string.Format("select Sno from {0} where Ymd = '{1}'", LottoryTbName, date);
                return conn.Query<int>(sql).ToList();
            }
        }
        #endregion

        #region [一星趋势]
        /// <summary>
        /// 获取星走势表名
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private static string getTendency1Table(Tendency1Enum type)
        {
            return "TendencyDigit" + (int)type + "_" + DateTime.Now.Year;
        }

        /// <summary>
        /// 创建一星趋势表
        /// </summary>
        /// <param name="tableName"></param>
        public static void CreateTendencyDigit1Table(string tableName)
        {
            string sql = string.Format("SELECT count(1) FROM information_schema.TABLES WHERE TABLE_SCHEMA ='{0}' and table_name ='{1}'", DbName, tableName);

            using (MySqlConnection conn = CreateConnection())
            {
                int count = conn.Query<int>(sql).FirstOrDefault();
                if (count == 0)
                {
                    sql = string.Format("CREATE TABLE {0} ( " +
                                   @"ID      int  auto_increment not null primary key," +
                                   @"Ymd     CHAR( 8 )      NOT NULL,	     " +
                                   @"Sno     CHAR( 4 )      NOT NULL,	     " +
                                   @"Lottery CHAR( 9 )      NOT NULL,	     " +
                                   @"Big     INT            NOT NULL,	     " +
                                   @"Small   INT            NOT NULL,	     " +
                                   @"Odd     INT            NOT NULL,	     " +
                                   @"Pair    INT            NOT NULL,	     " +
                                   @"Prime   INT            NOT NULL,	     " +
                                   @"Composite INT          NOT NULL,	     " +
                                   @"Big_1   INT            NOT NULL,	     " +
                                   @"Mid_1   INT            NOT NULL,	     " +
                                   @"Small_1 INT            NOT NULL,	     " +
                                   @"No_0    INT            NOT NULL,	     " +
                                   @"No_1    INT            NOT NULL,	     " +
                                   @"No_2    INT            NOT NULL,	     " +
                                   @"Dtime   CHAR( 12 )     NOT NULL 	     )", tableName);
                    conn.Execute(sql);

                    CreateIndex(tableName);
                }
            }
        }

        /// <summary>
        /// 新增一星走势数据
        /// </summary>
        /// <param name="type"></param>
        /// <param name="tendencys"></param>
        public static void SaveTendency1(Tendency1Enum type, List<TendencyModel> tendencys)
        {
            string tableName = getTendency1Table(type);
            using (MySqlConnection conn = CreateConnection())
            {
                conn.Open();
                MySqlTransaction trans = conn.BeginTransaction();

                string sqlCount = string.Format("SELECT count(1) FROM {0} where Ymd = @Ymd and Sno=@Sno", tableName);

                try
                {
                    string sql = null;
                    for (int i = 0; i < tendencys.Count; i++)
                    {
                        TendencyModel lm = tendencys[i];
                        int count = conn.Query<int>(sqlCount, lm).FirstOrDefault();
                        if (count == 0)
                        {
                            ///新增
                            sql = string.Format("insert into {0}         (Ymd      ," +
                                                                        @"Sno      ," +
                                                                        @"Lottery  ," +
                                                                        @"Big      ," +
                                                                        @"Small    ," +
                                                                        @"Odd      ," +
                                                                        @"Pair     ," +
                                                                        @"Prime    ," +
                                                                        @"Composite," +
                                                                        @"Big_1    ," +
                                                                        @"Mid_1    ," +
                                                                        @"Small_1  ," +
                                                                        @"No_0     ," +
                                                                        @"No_1     ," +
                                                                        @"No_2     ," +
                                                                        @"Dtime     )" +
                                                        @" VALUES (" +
                                                                        @"@Ymd      ," +
                                                                        @"@Sno      ," +
                                                                        @"@Lottery  ," +
                                                                        @"@Big      ," +
                                                                        @"@Small    ," +
                                                                        @"@Odd      ," +
                                                                        @"@Pair     ," +
                                                                        @"@Prime    ," +
                                                                        @"@Composite," +
                                                                        @"@Big_1    ," +
                                                                        @"@Mid_1    ," +
                                                                        @"@Small_1  ," +
                                                                        @"@No_0     ," +
                                                                        @"@No_1     ," +
                                                                        @"@No_2     ," +
                                                                        @"@Dtime     " +
                                                        @")", tableName);
                        }
                        else
                        {
                            ///修改
                            sql = string.Format("Update {0} set Big       =@Big      ," +
                                                                "Small    =@Small    ," +
                                                                "Odd      =@Odd      ," +
                                                                "Pair     =@Pair     ," +
                                                                "Prime    =@Prime    ," +
                                                                "Composite=@Composite," +
                                                                "Big_1    =@Big_1    ," +
                                                                "Mid_1    =@Mid_1    ," +
                                                                "Small_1  =@Small_1  ," +
                                                                "No_0     =@No_0     ," +
                                                                "No_1     =@No_1     ," +
                                                                "No_2     =@No_2     " +
                                                                "where Ymd = @Ymd and Sno=@Sno   ", tableName);
                        }
                        conn.Execute(sql, lm, trans);
                    }
                    trans.Commit();
                }
                catch (Exception er)
                {
                    trans.Rollback();
                }
            }
        }

        /// <summary>
        /// 查找一星走势
        /// </summary>
        /// <param name="type"></param>
        /// <param name="date"></param>
        /// <param name="topNum"></param>
        /// <returns></returns>
        public static List<TendencyModel> QueryTendency1(Tendency1Enum type, string date, int topNum)
        {
            string tableName = getTendency1Table(type);
            using (MySqlConnection conn = CreateConnection())
            {
                string sql = string.Format("select * from {0} where Ymd = '{1}' order by Sno desc limit 0,{2}", tableName, date, topNum);
                return conn.Query<TendencyModel>(sql).ToList();
            }
        }

        /// <summary>
        ///  查找指定期号
        /// </summary>
        /// <param name="type"></param>
        /// <param name="date"></param>
        /// <param name="sno"></param>
        /// <returns></returns>
        public static TendencyModel QueryTendency1(Tendency1Enum type, string date, string sno)
        {
            string tableName = getTendency1Table(type);
            using (MySqlConnection conn = CreateConnection())
            {
                string sql = string.Format("select * from {0} where Ymd = '{1}' and sno='{2}'", tableName, date, sno);
                return conn.Query<TendencyModel>(sql).FirstOrDefault();
            }
        }

        /// <summary>
        /// 通过日期区间查找1星走势图
        /// </summary>
        /// <param name="date">日期</param>
        /// <param name="topNum">最近期数</param>
        /// <returns></returns>
        public static List<TendencyModel> QueryTendency2Range(Tendency1Enum type, string startDate, string endDate)
        {
            string tableName = getTendency1Table(type);
            using (MySqlConnection conn = CreateConnection())
            {
                string sql = string.Format("select * from {0} where  Ymd  BETWEEN {1}  AND {2} order by ymd,Sno desc", tableName, startDate, endDate);
                var lt = conn.Query<TendencyModel>(sql).ToList();
                return lt;
            }
        }

        /// <summary>
        /// 一星最大走势
        /// </summary>
        /// <param name="type"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        public static TendencyModel QueryMaxTendency1(Tendency1Enum type, string date)
        {
            string tableName = getTendency1Table(type);
            using (MySqlConnection conn = CreateConnection())
            {
                string sql = string.Format("select   max(Big) Big,               " +
                                                    "max(Small) Small,          " +
                                                    "max(Odd) Odd,              " +
                                                    "max(Pair) Pair,            " +
                                                    "max(Prime) Prime,          " +
                                                    "max(Composite) Composite,  " +
                                                    "max(Big_1) Big_1,          " +
                                                    "max(Mid_1) Mid_1,          " +
                                                    "max(Small_1) Small_1,      " +
                                                    "max(No_0) No_0,            " +
                                                    "max(No_1) No_1,            " +
                                                    "max(No_2) No_2             " +
                                                    "from {0} where Ymd = '{1}' ", tableName, date);
                return conn.Query<TendencyModel>(sql).FirstOrDefault();
            }
        }

        #endregion

        #region [二星趋势]
        /// <summary>
        /// 获取二星走势表名
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private static string getTendency2Table(Tendency2Enum type)
        {
            string tableName = null;
            if (type == Tendency2Enum.Before)
                tableName = TendencyBefore2TbName;
            else
                tableName = TendencyAfter2TbName;
            return tableName;
        }

        /// <summary>
        /// 创建二星趋势表
        /// </summary>
        /// <param name="tableName"></param>
        public static void CreateTendency2Table(string tableName)
        {
            string sql = string.Format("SELECT count(1) FROM information_schema.TABLES WHERE TABLE_SCHEMA ='{0}' and table_name ='{1}'", DbName, tableName);

            using (MySqlConnection conn = CreateConnection())
            {
                int count = conn.Query<int>(sql).FirstOrDefault();
                if (count == 0)
                {
                    sql = string.Format("CREATE TABLE {0} ( " +
                                   @"ID      int  auto_increment not null primary key,     " +
                                   @"Ymd        CHAR( 8 )      NOT NULL,	     " +
                                   @"Sno        CHAR( 4 )      NOT NULL,	     " +
                                   @"Lottery    CHAR( 9 )      NOT NULL,	     " +
                                   @"Big        INT            NOT NULL,	     " +
                                   @"Small      INT            NOT NULL,	     " +
                                   @"BigSmall   INT            NOT NULL,	     " +
                                   @"SmallBig   INT            NOT NULL,	     " +
                                   @"Odd        INT            NOT NULL,	     " +
                                   @"Pair       INT            NOT NULL,	     " +
                                   @"OddPair    INT            NOT NULL,	     " +
                                   @"PairOdd    INT            NOT NULL,	     " +
                                   @"PrimePrime			    INT            NOT NULL,	     " +
                                   @"PrimeComposite		    INT            NOT NULL,	     " +
                                   @"CompositePrime		    INT            NOT NULL,	     " +
                                   @"CompositeComposite    INT            NOT NULL,	     " +
                                   @"Big1Big1	    INT            NOT NULL,	     " +
                                   @"Big1Mid1	    INT            NOT NULL,	     " +
                                   @"Big1Small1	    INT            NOT NULL,	     " +
                                   @"Mid1Big1	   INT            NOT NULL,	     " +
                                   @"Mid1Mid1	    INT            NOT NULL,	     " +
                                   @"Mid1Small1	    INT            NOT NULL,	     " +
                                   @"Small1Big1	    INT            NOT NULL,	     " +
                                   @"Small1Mid1	   INT            NOT NULL,	     " +
                                   @"Small1Small1    INT            NOT NULL,	     " +
                                   @"No_00    INT            NOT NULL,	     " +
                                   @"No_01    INT            NOT NULL,	     " +
                                   @"No_02   INT            NOT NULL,	     " +
                                   @"No_10    INT            NOT NULL,	     " +
                                   @"No_11    INT            NOT NULL,	     " +
                                   @"No_12    INT            NOT NULL,	     " +
                                   @"No_20   INT            NOT NULL,	     " +
                                   @"No_21    INT            NOT NULL,	     " +
                                   @"No_22    INT            NOT NULL,	     " +
                                   @"Dbl        INT            NOT NULL,	     " +
                                   @"Dtime      CHAR( 12 )      NOT NULL 	     )", tableName);
                    conn.Execute(sql);

                    CreateIndex(tableName);
                }
            }
        }

        /// <summary>
        /// 新增二星走势数据
        /// </summary>
        /// <param name="lotterys"></param>
        public static void SaveTendency2(Tendency2Enum type, List<Tendency2Model> lotterys)
        {
            string tableName = getTendency2Table(type);
            using (MySqlConnection conn = CreateConnection())
            {
                conn.Open();
                MySqlTransaction trans = conn.BeginTransaction();

                string sqlCount = string.Format("SELECT count(1) FROM {0} where Ymd = @Ymd and Sno=@Sno", tableName);

                try
                {
                    string sql = null;
                    for (int i = 0; i < lotterys.Count; i++)
                    {
                        Tendency2Model lm = lotterys[i];
                        int count = conn.Query<int>(sqlCount, lm).FirstOrDefault();
                        if (count == 0)
                        {
                            ///新增
                            sql = string.Format("insert into {0}(Ymd      ," +
                                                                        @"Sno      ," +
                                                                        @"Lottery  ," +
                                                                        @"Big      ," +
                                                                        @"Small    ," +
                                                                        @"BigSmall ," +
                                                                        @"SmallBig ," +
                                                                        @"Odd      ," +
                                                                        @"Pair     ," +
                                                                        @"OddPair  ," +
                                                                        @"PairOdd  ," +
                                                                        @"PrimePrime," +
                                                                        @"PrimeComposite," +
                                                                        @"CompositePrime," +
                                                                        @"CompositeComposite," +
                                                                        @"Big1Big1," +
                                                                        @"Big1Mid1," +
                                                                        @"Big1Small1," +
                                                                        @"Mid1Big1," +
                                                                        @"Mid1Mid1," +
                                                                        @"Mid1Small1," +
                                                                        @"Small1Big1," +
                                                                        @"Small1Mid1," +
                                                                        @"Small1Small1," +
                                                                        @"No_00," +
                                                                        @"No_01," +
                                                                        @"No_02," +
                                                                        @"No_10," +
                                                                        @"No_11," +
                                                                        @"No_12," +
                                                                        @"No_20," +
                                                                        @"No_21," +
                                                                        @"No_22," +
                                                                        @"Dbl      ," +
                                                                        @"Dtime     )" +
                                                        @" VALUES (" +
                                                                        @"@Ymd      ," +
                                                                        @"@Sno      ," +
                                                                        @"@Lottery  ," +
                                                                        @"@Big      ," +
                                                                        @"@Small    ," +
                                                                        @"@BigSmall ," +
                                                                        @"@SmallBig ," +
                                                                        @"@Odd      ," +
                                                                        @"@Pair     ," +
                                                                        @"@OddPair  ," +
                                                                        @"@PairOdd  ," +
                                                                        @"@PrimePrime," +
                                                                        @"@PrimeComposite," +
                                                                        @"@CompositePrime," +
                                                                        @"@CompositeComposite," +
                                                                        @"@Big1Big1," +
                                                                        @"@Big1Mid1," +
                                                                        @"@Big1Small1," +
                                                                        @"@Mid1Big1," +
                                                                        @"@Mid1Mid1," +
                                                                        @"@Mid1Small1," +
                                                                        @"@Small1Big1," +
                                                                        @"@Small1Mid1," +
                                                                        @"@Small1Small1," +
                                                                        @"@No_00," +
                                                                        @"@No_01," +
                                                                        @"@No_02," +
                                                                        @"@No_10," +
                                                                        @"@No_11," +
                                                                        @"@No_12," +
                                                                        @"@No_20," +
                                                                        @"@No_21," +
                                                                        @"@No_22," +
                                                                        @"@Dbl      ," +
                                                                        @"@Dtime     " +
                                                        @")", tableName);

                        }
                        else
                        {
                            ///修改
                            sql = string.Format("Update {0} set Big       =@Big      ," +
                                                                "Small    =@Small    ," +
                                                                "BigSmall =@BigSmall ," +
                                                                "SmallBig =@SmallBig ," +
                                                                "Odd      =@Odd      ," +
                                                                "Pair     =@Pair     ," +
                                                                "OddPair  =@OddPair  ," +
                                                                "PairOdd  =@PairOdd  ," +
                                                                "PrimePrime        		=@PrimePrime          ," +
                                                                "PrimeComposite    		=@PrimeComposite      ," +
                                                                "CompositePrime    		=@CompositePrime      ," +
                                                                "CompositeComposite		=@CompositeComposite  ," +
                                                                "Big1Big1          		=@Big1Big1            ," +
                                                                "Big1Mid1          		=@Big1Mid1            ," +
                                                                "Big1Small1        		=@Big1Small1          ," +
                                                                "Mid1Big1          		=@Mid1Big1            ," +
                                                                "Mid1Mid1          		=@Mid1Mid1            ," +
                                                                "Mid1Small1        		=@Mid1Small1          ," +
                                                                "Small1Big1        		=@Small1Big1          ," +
                                                                "Small1Mid1        		=@Small1Mid1          ," +
                                                                "Small1Small1      		=@Small1Small1        ," +
                                                                "No_00             		=@No_00               ," +
                                                                "No_01             		=@No_01               ," +
                                                                "No_02             		=@No_02               ," +
                                                                "No_10             		=@No_10               ," +
                                                                "No_11             		=@No_11               ," +
                                                                "No_12             		=@No_12               ," +
                                                                "No_20             		=@No_20               ," +
                                                                "No_21             		=@No_21               ," +
                                                                "No_22             		=@No_22               ," +
                                                                "Dbl      =@Dbl       " +
                                                                "where Ymd = @Ymd and Sno=@Sno   ", tableName);
                        }
                        conn.Execute(sql, lm, trans);
                    }
                    trans.Commit();
                }
                catch (Exception er)
                {
                    trans.Rollback();
                }
            }
        }

        /// <summary>
        ///  查找指定期号
        /// </summary>
        /// <param name="type"></param>
        /// <param name="date"></param>
        /// <param name="sno"></param>
        /// <returns></returns>
        public static Tendency2Model QueryTendency2(Tendency2Enum type, string date, string sno)
        {
            string tableName = getTendency2Table(type);
            using (MySqlConnection conn = CreateConnection())
            {
                string sql = string.Format("select * from {0} where Ymd = '{1}' and sno='{2}'", tableName, date, sno);
                return conn.Query<Tendency2Model>(sql).FirstOrDefault();
            }
        }

        /// <summary>
        /// 近期走势
        /// </summary>
        /// <param name="date">日期</param>
        /// <param name="topNum">最近期数</param>
        /// <returns></returns>
        public static List<Tendency2Model> QueryTendency2(Tendency2Enum type, string date, int topNum)
        {
            string tableName = getTendency2Table(type);
            using (MySqlConnection conn = CreateConnection())
            {
                string sql = string.Format("select * from {0} where Ymd = '{1}' order by Sno desc limit 0,{2}", tableName, date, topNum);
                var lt = conn.Query<Tendency2Model>(sql).ToList();
                return lt;
            }
        }

        /// <summary>
        /// 日期区间走势
        /// </summary>
        /// <param name="date">日期</param>
        /// <param name="topNum">最近期数</param>
        /// <returns></returns>
        public static List<Tendency2Model> QueryTendency2Range(Tendency2Enum type, string startDate, string endDate)
        {
            string tableName = getTendency2Table(type);
            using (MySqlConnection conn = CreateConnection())
            {
                string sql = string.Format("select * from {0} where  Ymd  BETWEEN {1}  AND {2} order by ymd,Sno desc", tableName, startDate, endDate);
                var lt = conn.Query<Tendency2Model>(sql).ToList();
                return lt;
            }
        }

        /// <summary>
        /// 二星最大走势
        /// </summary>
        /// <param name="type"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        public static Tendency2Model QueryMaxTendency2(Tendency2Enum type, string date)
        {
            string tableName = getTendency2Table(type);
            using (MySqlConnection conn = CreateConnection())
            {
                string sql = string.Format("select  max(Big) Big,                                " +
                                                    "max(Small) Small,                           " +
                                                    "max(BigSmall) BigSmall,                     " +
                                                    "max(SmallBig) SmallBig,                     " +
                                                    "max(Odd) Odd,                               " +
                                                    "max(Pair) Pair,                             " +
                                                    "max(OddPair) OddPair,                       " +
                                                    "max(PairOdd) PairOdd,                       " +
                                                    "max(PrimePrime) PrimePrime,                 " +
                                                    "max(PrimeComposite) PrimeComposite,         " +
                                                    "max(CompositePrime) CompositePrime,         " +
                                                    "max(CompositeComposite) CompositeComposite, " +
                                                    "max(Big1Big1) Big1Big1,                     " +
                                                    "max(Big1Mid1) Big1Mid1,                     " +
                                                    "max(Big1Small1) Big1Small1,                 " +
                                                    "max(Mid1Big1) Mid1Big1,                     " +
                                                    "max(Mid1Mid1) Mid1Mid1,                     " +
                                                    "max(Mid1Small1) Mid1Small1,                 " +
                                                    "max(Small1Big1) Small1Big1,                 " +
                                                    "max(Small1Mid1) Small1Mid1,                 " +
                                                    "max(Small1Small1) Small1Small1,             " +
                                                    "max(No_00) 	No_00 	,                    " +
                                                    "max(No_01) 	No_01		,                " +
                                                    "max(No_02) 	No_02		,                " +
                                                    "max(No_10) 	No_10		,                " +
                                                    "max(No_11) 	No_11		,                " +
                                                    "max(No_12) 	No_12		,                " +
                                                    "max(No_20) 	No_20		,                " +
                                                    "max(No_21) 	No_21		,                " +
                                                    "max(No_22) 	No_22		,                " +
                                                    "max(Dbl) Dbl                                " +
                                                    " from {0} where Ymd = '{1}'", tableName, date);

                return conn.Query<Tendency2Model>(sql).FirstOrDefault();
            }
        }
        #endregion

        #region [前二后二包胆趋势]
        public static void CreateTendency1Table(string tableName)
        {
            string sql = string.Format("SELECT count(1) FROM information_schema.TABLES WHERE TABLE_SCHEMA ='{0}' and table_name ='{1}'", DbName, tableName);

            using (MySqlConnection conn = CreateConnection())
            {
                int count = conn.Query<int>(sql).FirstOrDefault();
                if (count == 0)
                {
                    sql = string.Format("CREATE TABLE {0} ( " +
                                   @"ID      int  auto_increment not null primary key,     " +
                                   @"Ymd     CHAR( 8 )      NOT NULL,	     " +
                                   @"Sno     CHAR( 4 )      NOT NULL,	     " +
                                   @"Lottery CHAR( 9 )      NOT NULL,	     " +
                                   @"Num0    INT            NOT NULL,	     " +
                                   @"Num1    INT            NOT NULL,	     " +
                                   @"Num2    INT            NOT NULL,	     " +
                                   @"Num3    INT            NOT NULL,	     " +
                                   @"Num4    INT            NOT NULL,	     " +
                                   @"Num5    INT            NOT NULL,	     " +
                                   @"Num6    INT            NOT NULL,	     " +
                                   @"Num7    INT            NOT NULL,	     " +
                                   @"Num8    INT            NOT NULL,	     " +
                                   @"Num9    INT            NOT NULL,	     " +
                                   @"Dtime   CHAR( 12 )      NOT NULL 	     )", tableName);
                    conn.Execute(sql);

                    CreateIndex(tableName);
                }
            }
        }


        public static void SaveTendencyDigit1(List<Tendency1Model> lotterys)
        {
            using (MySqlConnection conn = CreateConnection())
            {
                conn.Open();
                MySqlTransaction trans = conn.BeginTransaction();

                string sqlCount = string.Format("SELECT count(1) FROM {0} where Ymd = @Ymd and Sno=@Sno", Tendency1TbName);

                try
                {
                    string sql = null;
                    for (int i = 0; i < lotterys.Count; i++)
                    {
                        Tendency1Model lm = lotterys[i];

                        int count = conn.Query<int>(sqlCount, lm).FirstOrDefault();
                        if (count == 0)
                        {
                            ///新增
                            sql = string.Format("insert into {0}(Ymd      ," +
                                                                        @"Sno      ," +
                                                                        @"Lottery  ," +
                                                                        @"Num0 ," +
                                                                        @"Num1 ," +
                                                                        @"Num2 ," +
                                                                        @"Num3 ," +
                                                                        @"Num4 ," +
                                                                        @"Num5 ," +
                                                                        @"Num6 ," +
                                                                        @"Num7 ," +
                                                                        @"Num8 ," +
                                                                        @"Num9 ," +
                                                                        @"Dtime     )" +
                                                        @" VALUES (" +
                                                                        @"@Ymd      ," +
                                                                        @"@Sno      ," +
                                                                        @"@Lottery  ," +
                                                                        @"@Num0 ," +
                                                                        @"@Num1 ," +
                                                                        @"@Num2 ," +
                                                                        @"@Num3 ," +
                                                                        @"@Num4 ," +
                                                                        @"@Num5 ," +
                                                                        @"@Num6 ," +
                                                                        @"@Num7 ," +
                                                                        @"@Num8 ," +
                                                                        @"@Num9 ," +
                                                                        @"@Dtime     " +
                                                        @")", Tendency1TbName);
                        }
                        else
                        {
                            ///修改
                            sql = string.Format("Update {0} set  Num0  = @Num0 ," +
                                                                "Num1  = @Num1 ," +
                                                                "Num2  = @Num2 ," +
                                                                "Num3  = @Num3 ," +
                                                                "Num4  = @Num4 ," +
                                                                "Num5  = @Num5 ," +
                                                                "Num6  = @Num6 ," +
                                                                "Num7  = @Num7 ," +
                                                                "Num8  = @Num8 ," +
                                                                "Num9  = @Num9 ," +
                                                                "Dtime = @Dtime " +
                                                                "where Ymd = @Ymd and Sno=@Sno   ", Tendency1TbName);
                        }
                        conn.Execute(sql, lm, trans);
                    }
                    trans.Commit();
                }
                catch (Exception er)
                {
                    trans.Rollback();
                }
            }
        }

        public static Tendency1Model QueryTendencyDigit1(string date, string sno)
        {
            using (MySqlConnection conn = CreateConnection())
            {
                string sql = string.Format("select * from {0} where Ymd = '{1}' and sno='{2}'", Tendency1TbName, date, sno);
                return conn.Query<Tendency1Model>(sql).FirstOrDefault();
            }
        }

        public static List<Tendency1Model> QueryTendencyDigit1(string date, int topNum)
        {
            using (MySqlConnection conn = CreateConnection())
            {
                string sql = string.Format("select * from {0} where Ymd = '{1}' order by Sno desc limit 0,{2}", Tendency1TbName, date, topNum);
                var lt = conn.Query<Tendency1Model>(sql).ToList();
                return lt;
            }
        }

        public static Tendency1Model QueryMaxTendencyDigit1(string startDate, string endDate)
        {
            using (MySqlConnection conn = CreateConnection())
            {
                string sql = string.Format("select  max(Num0) Num0 ," +
                                                  " max(Num1) Num1 ," +
                                                  " max(Num2) Num2 ," +
                                                  " max(Num3) Num3 ," +
                                                  " max(Num4) Num4 ," +
                                                  " max(Num5) Num5 ," +
                                                  " max(Num6) Num6 ," +
                                                  " max(Num7) Num7 ," +
                                                  " max(Num8) Num8 ," +
                                                  " max(Num9) Num9 " +
                                                  " from {0} where  Ymd  BETWEEN {1}  AND {2}", Tendency1TbName, startDate, endDate);
                return conn.Query<Tendency1Model>(sql).FirstOrDefault();
            }
        }
        #endregion

        #region [全部数字趋势]
        public static void CreateAllTendency1Table(string tableName)
        {
            string sql = string.Format("SELECT count(1) FROM information_schema.TABLES WHERE TABLE_SCHEMA ='{0}' and table_name ='{1}'", DbName, tableName);

            using (MySqlConnection conn = CreateConnection())
            {
                int count = conn.Query<int>(sql).FirstOrDefault();
                if (count == 0)
                {
                    sql = string.Format("CREATE TABLE {0} ( " +
                                   @"ID      int  auto_increment not null primary key,     " +
                                   @"Ymd     CHAR( 8 )      NOT NULL,	     " +
                                   @"Sno     CHAR( 4 )      NOT NULL,	     " +
                                   @"Lottery CHAR( 9 )      NOT NULL,	     " +
                                   @"Num0    INT            NOT NULL,	     " +
                                   @"Num1    INT            NOT NULL,	     " +
                                   @"Num2    INT            NOT NULL,	     " +
                                   @"Num3    INT            NOT NULL,	     " +
                                   @"Num4    INT            NOT NULL,	     " +
                                   @"Num5    INT            NOT NULL,	     " +
                                   @"Num6    INT            NOT NULL,	     " +
                                   @"Num7    INT            NOT NULL,	     " +
                                   @"Num8    INT            NOT NULL,	     " +
                                   @"Num9    INT            NOT NULL,	     " +
                                   @"Dtime   CHAR( 12 )      NOT NULL 	     )", tableName);
                    conn.Execute(sql);

                    CreateIndex(tableName);
                }
            }
        }

        public static void SaveAllTendencyDigit1(List<Tendency1Model> lotterys)
        {
            using (MySqlConnection conn = CreateConnection())
            {
                conn.Open();
                MySqlTransaction trans = conn.BeginTransaction();

                string sqlCount = string.Format("SELECT count(1) FROM {0} where Ymd = @Ymd and Sno=@Sno", TendencyAllTbName);

                try
                {
                    string sql = null;
                    for (int i = 0; i < lotterys.Count; i++)
                    {
                        Tendency1Model lm = lotterys[i];
                        int count = conn.Query<int>(sqlCount, lm).FirstOrDefault();
                        if (count == 0)
                        {
                            ///新增
                            sql = string.Format("insert into {0}(Ymd      ," +
                                                                        @"Sno      ," +
                                                                        @"Lottery  ," +
                                                                        @"Num0 ," +
                                                                        @"Num1 ," +
                                                                        @"Num2 ," +
                                                                        @"Num3 ," +
                                                                        @"Num4 ," +
                                                                        @"Num5 ," +
                                                                        @"Num6 ," +
                                                                        @"Num7 ," +
                                                                        @"Num8 ," +
                                                                        @"Num9 ," +
                                                                        @"Dtime     )" +
                                                        @" VALUES (" +
                                                                        @"@Ymd      ," +
                                                                        @"@Sno      ," +
                                                                        @"@Lottery  ," +
                                                                        @"@Num0 ," +
                                                                        @"@Num1 ," +
                                                                        @"@Num2 ," +
                                                                        @"@Num3 ," +
                                                                        @"@Num4 ," +
                                                                        @"@Num5 ," +
                                                                        @"@Num6 ," +
                                                                        @"@Num7 ," +
                                                                        @"@Num8 ," +
                                                                        @"@Num9 ," +
                                                                        @"@Dtime     " +
                                                        @")", TendencyAllTbName);
                        }
                        else
                        {
                            ///修改
                            sql = string.Format("Update {0} set  Num0  = @Num0 ," +
                                                                "Num1  = @Num1 ," +
                                                                "Num2  = @Num2 ," +
                                                                "Num3  = @Num3 ," +
                                                                "Num4  = @Num4 ," +
                                                                "Num5  = @Num5 ," +
                                                                "Num6  = @Num6 ," +
                                                                "Num7  = @Num7 ," +
                                                                "Num8  = @Num8 ," +
                                                                "Num9  = @Num9 ," +
                                                                "Dtime = @Dtime " +
                                                                "where Ymd = @Ymd and Sno=@Sno   ", TendencyAllTbName);
                        }
                        conn.Execute(sql, lm, trans);
                    }
                    trans.Commit();
                }
                catch (Exception er)
                {
                    trans.Rollback();
                }
            }
        }

        public static Tendency1Model QueryAllTendencyDigit1(string date, string sno)
        {
            using (MySqlConnection conn = CreateConnection())
            {
                string sql = string.Format("select * from {0} where Ymd = '{1}' and sno='{2}'", TendencyAllTbName, date, sno);
                return conn.Query<Tendency1Model>(sql).FirstOrDefault();
            }
        }

        public static List<Tendency1Model> QueryAllTendencyDigit1(string date, int topNum)
        {
            using (MySqlConnection conn = CreateConnection())
            {
                string sql = string.Format("select * from {0} where Ymd = '{1}' order by Sno desc limit 0,{2}", TendencyAllTbName, date, topNum);
                var lt = conn.Query<Tendency1Model>(sql).ToList();
                return lt;
            }
        }

        public static Tendency1Model QueryAllMaxTendencyDigit1(string startDate, string endDate)
        {
            using (MySqlConnection conn = CreateConnection())
            {
                string sql = string.Format("select  max(Num0) Num0 ," +
                                                  " max(Num1) Num1 ," +
                                                  " max(Num2) Num2 ," +
                                                  " max(Num3) Num3 ," +
                                                  " max(Num4) Num4 ," +
                                                  " max(Num5) Num5 ," +
                                                  " max(Num6) Num6 ," +
                                                  " max(Num7) Num7 ," +
                                                  " max(Num8) Num8 ," +
                                                  " max(Num9) Num9 " +
                                                  " from {0} where  Ymd  BETWEEN {1}  AND {2}", TendencyAllTbName, startDate, endDate);
                return conn.Query<Tendency1Model>(sql).FirstOrDefault();
            }
        }
        #endregion
    }
}

