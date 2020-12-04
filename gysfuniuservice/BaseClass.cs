using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Collections;
using System.Data.SqlClient;
using System.Configuration;
using System.Data;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace gysfuniuservice
{
    public class BaseClass
    {

        #region SQLSERVER操作
        //获取数据库链接
        public SqlConnection Get_con()
        {
                SqlConnection con = new SqlConnection(ConfigurationManager.AppSettings["DBConncetion"]);
            return con;
        }

        //获取数据库链接
        public SqlConnection Get_con(string strYear, string strFactory)
        {
            string strSql = "select dbname,dbAddress,dbuserid,dbpwd from G_factory where factory = '"+ strFactory.Trim()
                + "' and year = '"+ strYear.Trim() + "'";
            SqlConnection con =new SqlConnection();
            try
            {
                DataTable dt = SearchTableSql(strSql);
               
                   con = new SqlConnection("data source = " + dt.Rows[0]["dbAddress"].ToString().Trim()
                        + "; user id = " + dt.Rows[0]["dbuserid"].ToString().Trim()
                        + "; pwd = " + dt.Rows[0]["dbpwd"].ToString().Trim()
                        + "; database = " + dt.Rows[0]["dbname"].ToString().Trim() + ";Max Pool Size=1024;");
              
                return con;
            }
            catch(Exception e)
            { return con; }
        }

        //执行SQLSERVER数据库插入\更新\删除操作
        public bool ExecSql(string strSql, string strYear, string strFactory)
        {
            try
            {
                SqlConnection con = Get_con(strYear, strFactory);
                con.Open();
                SqlTransaction transaction = null;
                transaction = con.BeginTransaction();
                try
                {
                    SqlCommand cmd = new SqlCommand(strSql, con);
                    cmd.Connection = con;
                    cmd.CommandTimeout = 0;
                    cmd.Transaction = transaction;
                    cmd.ExecuteNonQuery();
                    transaction.Commit();
                    con.Close();
                    cmd.Dispose();
                    return true;
                }
                catch (Exception e)
                {
                    transaction.Rollback();
                    // WriteLog(e.Message.ToString() + strSql);
                    //con.Close();
                    //cmd.Dispose();
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }


        //执行SQLSERVER数据库插入\更新\删除操作
        public bool ExecListSql(ArrayList listSql,string strYear, string strFactory)
        {
            try
            {
                string strSql1 = "";
                SqlConnection con = Get_con(strYear, strFactory);
                con.Open();
                SqlTransaction transaction = null;
                transaction = con.BeginTransaction();
                try
                {
                    SqlCommand cmd;
                    foreach (string strSql in listSql)
                    {
                        strSql1 = strSql;
                        cmd = new SqlCommand(strSql, con);
                        cmd.Connection = con;
                        cmd.Transaction = transaction;
                        cmd.ExecuteNonQuery();
                        cmd.CommandTimeout = 0;
                    }
                    transaction.Commit();
                    con.Close();
                    return true;
                }
                catch (Exception e)
                {
                    transaction.Rollback();
                    // WriteLog(e.Message.ToString() + strSql1);
                    return false;
                }
            }
            catch 
            {
                return false;
            }
        }


        // 查询SQLSERVER返回单条数据以‘@’ 号隔开
        public string SearchOneSql(string strSql,string strYear, string strFactory)
        {
            string strReturn = "";
            try
            {
                SqlConnection con = Get_con(strYear, strFactory);
                con.Open();
                SqlCommand cmd = new SqlCommand(strSql, con);
                try
                {
                    cmd.CommandTimeout = 0;
                    cmd.ExecuteNonQuery();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            for (int i = 0; i < dr.FieldCount; i++)
                            {
                                strReturn += dr[i].ToString() + "@";
                            }
                            strReturn = strReturn.Substring(0, strReturn.Length - 1);
                        }
                    }
                    con.Close();
                    cmd.Dispose();
                }
                catch (Exception e)
                {
                    // WriteLog(e.Message.ToString());
                }
            }     
            catch 
            {
                return strReturn;
            }
            return strReturn;
            
        }

        // 查询SQLSERVER返回单条数据以‘@’ 号隔开
        public string SearchOneSql(string strSql)
        {
            string strReturn = "";
            try
            {
                SqlConnection con = Get_con();
                con.Open();
                SqlCommand cmd = new SqlCommand(strSql, con);
                try
                {
                    cmd.CommandTimeout = 0;
                    cmd.ExecuteNonQuery();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            for (int i = 0; i < dr.FieldCount; i++)
                            {
                                strReturn += dr[i].ToString() + "@";
                            }
                            strReturn = strReturn.Substring(0, strReturn.Length - 1);
                        }
                    }
                    con.Close();
                    cmd.Dispose();
                }
                catch (Exception e)
                {
                    // WriteLog(e.Message.ToString());
                }
            }
            catch
            {
                return strReturn;
            }
            return strReturn;
        }

        // 查询SQLSERVER返回DataTable
        public DataTable SearchTableSql(string strSql,string strYear, string strFactory)
        {
            DataSet ds = new DataSet();
            DataTable dt = new DataTable();
            try
            {
                SqlConnection con = Get_con(strYear, strFactory);
                con.Open();
                SqlDataAdapter sda = new SqlDataAdapter(strSql, con);
                sda.SelectCommand.CommandTimeout = 0;
                try
                {
                    sda.Fill(ds);
                    if (ds.Tables.Count > 0)
                    {
                        dt = ds.Tables[0];
                    }
                    con.Close();
                    sda.Dispose();
                }
                catch (Exception e)
                {
                    // WriteLog(e.Message.ToString());
                    throw e;
                }
            }
            catch
            {
                return dt;
            }
            return dt;
        }

        // 查询SQLSERVER返回DataTable
        public DataTable SearchTableSql(string strSql)
        {
            DataSet ds = new DataSet();
            DataTable dt = new DataTable();
            try
            {
                SqlConnection con = Get_con();
                con.Open();
                SqlDataAdapter sda = new SqlDataAdapter(strSql, con);
                sda.SelectCommand.CommandTimeout = 0;
                try
                {
                    sda.Fill(ds);
                    if (ds.Tables.Count > 0)
                    {
                        dt = ds.Tables[0];
                    }
                    con.Close();
                    sda.Dispose();
                }
                catch (Exception e)
                {
                    // WriteLog(e.Message.ToString());
                    throw e;
                }
            }
            catch
            {
                return dt;
            }
            return dt;
        }

        // 查询SQLSERVER返回多条数据，每一条以‘@’ 号隔开
        public ArrayList SearchMulSql(string strSql, string strYear, string strFactory)
        {
            ArrayList strReturn = new ArrayList();
            string strValue = "";
            try
            {
                SqlConnection con = Get_con(strYear, strFactory);
                con.Open();
                SqlCommand cmd = new SqlCommand(strSql, con);
                try
                {
                    cmd.CommandTimeout = 0;
                    cmd.ExecuteNonQuery();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            for (int i = 0; i < dr.FieldCount; i++)
                            {
                                strValue += dr[i].ToString() + "@";
                            }

                            strValue = strValue.Substring(0, strValue.Length - 1);
                            strReturn.Add(strValue);
                            strValue = "";
                        }
                    }
                    con.Close();
                    cmd.Dispose();
                }
                catch (Exception e)
                {
                    //WriteLog(e.Message.ToString());
                    con.Close();
                    cmd.Dispose();
                    throw e;
                }
            }
            catch
            {
                return strReturn;
            }
            return strReturn;
        }


        //更新SQLSERVER表：strTable,返回受影响行数
        public int UpdateSql(string strSql,string strYear, string strFactory)
        {
            try
            {
                SqlConnection con = Get_con(strYear, strFactory);
                con.Open();
                SqlCommand cmd = new SqlCommand(strSql, con);
                cmd.CommandTimeout = 0;
                return cmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
               // WriteLog(e.Message.ToString());
                throw e;
            }
        }


        /// <summary>
        /// 更新SQLSERVER表
        /// strTable：update tablename ; listCol: list of update column`name and column`value ,split by ',';
        /// listCol : list of set column`name and column`value ,split by ',';
        /// listWhere：list of where column`name and column`value ,split by ',';
        /// if update fail ,return false
        /// </summary>
        //更新SQLSERVER表：strTable
        public bool UpdateSql(string strTable, ArrayList listCol, ArrayList listWhere,string strYear, string strFactory)
        {
            string strSql = "update strTable set ";
            for (int i = 0; i < listCol.Count; i++)
            {
                strSql += listCol[i].ToString().Split(',')[0].ToString() + " = '" + listCol[i].ToString().Split(',')[1].ToString() + "' , ";
            }
            strSql = strSql.Substring(0, strSql.Length - 2);

            if (listWhere.Count > 0)
            {
                strSql += " where ";
            }

            for (int i = 0; i < listWhere.Count; i++)
            {
                strSql += listWhere[i].ToString().Split(',')[0].ToString() + " = '" + listWhere[i].ToString().Split(',')[1].ToString() + "' and ";
            }
            strSql = strSql.Substring(0, strSql.Length - 4);
            try
            {
                return ExecSql(strSql, strYear,  strFactory);
            }
            catch (Exception e)
            {
               // WriteLog(e.Message.ToString());
                throw e;
            }
        }

        #endregion

        //读取txt文件的内容
        public string GetInterIDList(string strfile)
        {
            string strout;
            strout = "";
            if (!File.Exists(System.Web.HttpContext.Current.Server.MapPath(strfile)))
            {
                strout = "ERROR";
            }
            else
            {
                StreamReader sr = new StreamReader(System.Web.HttpContext.Current.Server.MapPath(strfile), System.Text.Encoding.Default);
                String input = sr.ReadToEnd();
                sr.Close();
                strout = input;
            }
            return strout;
        }


        //序列化
        public byte[] SerializeObject(object pObj)
        {
            if (pObj == null)
                return null;
            System.IO.MemoryStream memoryStream = new System.IO.MemoryStream();
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(memoryStream, pObj);
            memoryStream.Position = 0;
            byte[] read = new byte[memoryStream.Length];
            memoryStream.Read(read, 0, read.Length);
            memoryStream.Close();
            return read;
        }

        //反序列化
        public object DeserializeObject(byte[] pBytes)
        {
            object newOjb = null;
            if (pBytes == null)
            {
                return newOjb;
            }


            System.IO.MemoryStream memoryStream = new System.IO.MemoryStream(pBytes);
            memoryStream.Position = 0;
            BinaryFormatter formatter = new BinaryFormatter();
            newOjb = formatter.Deserialize(memoryStream);
            memoryStream.Close();


            return newOjb;
        }
    }
}