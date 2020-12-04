using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using System.Web.Script.Serialization;
using System.Web.Script.Services;
using Newtonsoft.Json;
using System.Data;
using System.Data.SqlClient;
using System.Collections;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;
using System.Web.Services.Protocols;
namespace gysfuniuservice
{
    /// <summary>
    /// WebService1 的摘要说明
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // 若要允许使用 ASP.NET AJAX 从脚本中调用此 Web 服务，请取消对下行的注释。
    // [System.Web.Script.Services.ScriptService]
    public class WebService1 : System.Web.Services.WebService
    {
        BaseClass bc = new BaseClass();

        #region api登录验证
        public MySoapHeader header;
        /// <summary>
        /// 验证是安全令牌是否正确
        /// </summary>
        /// <returns></returns>
        private bool IsValiToken()
        {
            bool flag = false;
            string userName = header.UserName;
            string passWord = header.PassWord;

            if (userName == "cc" && passWord == "admin&cc")
            {
                flag = true;
            }
            return flag;
            //return true;
        }
        #endregion

        //[WebMethod]
        //public string HelloWorld()
        //{
        //    return "Hello World";
        //}

        //获取入库单数量
        /// <summary>
        /// 查询用友数据库中是否存在入库单号
        /// 输入： 入库单号
        /// 返回： （存在入库单，返回true；不存在入库单，返回false）
        /// </summary>
        [WebMethod]
        [System.Web.Services.Protocols.SoapHeader("header")]//用户身份验证的soap头
        public bool GetListExist(string strCcode, string strYear, string strFactory)
        {
            if (this.IsValiToken())
            {
                string strSql = "select count(*) from FN_V_RDList where ccode = '" + strCcode.Trim() + "'";

                string strReturn = bc.SearchOneSql(strSql, strYear, strFactory);
                if (strReturn == "0")
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            return true;
        }

        //获取入库单
        /// <summary>
        /// 查询用友数据库中入库单明细
        /// 输入： 入库单号
        /// 返回： list
        /// </summary>
        [WebMethod]
        [System.Web.Services.Protocols.SoapHeader("header")]//用户身份验证的soap头
        public string GetList(string strCcode, string strFactory, string strYear, string strVendor, string strDateB, string strDateE, string strUsercode)
        {
            List<RDlist> list = new List<RDlist>();
            if (this.IsValiToken())
            {
                string strSql = "";
                string strSql1 = "";
                string strSql2 = "";
                strSql1 = "select top 1000  Autoid,Ccode  ,CONVERT(varchar(100), ddate, 23) AS Ddate,Cvencode,Cvenname,cinvcode,Cinvname,Iquantity,Cinvm_unit,Price,cwhcode,cwhname,ZKQ_Price,isnull(Kl,1) as Kl,isnull(itaxrate,0) as itaxrate,cpersoncode from FN_V_RDList "
                       + " where  ( fapiaohao = '' or fapiaohao is null ) and  ( sffb = '' or sffb is null )   ";

                if (strUsercode.Trim() != "")
                {
                    string strUserCodeBd = GetUserbd(strUsercode);
                    if (strUserCodeBd == "")
                    {
                        strSql1 += " and ( cpersoncode = '" + strUsercode + "' or cpersoncode = '' or cpersoncode is null)  ";
                    }
                    else
                    {
                        strSql1 += " and ( cpersoncode in (" + strUserCodeBd + ") or cpersoncode = '' or cpersoncode is null)  ";
                    }
                }
                if (strCcode.Trim() != "")
                {
                    strSql1 += " and ccode = '" + strCcode.Trim() + "'";
                }
                if (strVendor.Trim() != "")
                {
                    strSql1 += " and cVenCode = '" + strVendor.Trim() + "'";
                }
                strSql1 += " and Ddate >= cast('" + strDateB.Trim() + "' as datetime) AND Ddate <= cast('" + strDateE.Trim() + "' as datetime)";

                strSql2 = strSql1;
                strSql1 += " and Iquantity < 0 order by ccode asc";
                strSql2 += " and Iquantity >= 0 order by ccode asc";
                strSql = "SELECT * FROM( " + strSql1 + " union all " + strSql2 + " ) t1;";

                DataTable dt = bc.SearchTableSql(strSql, strYear, strFactory);
                foreach (DataRow dr in dt.Rows)
                {
                    RDlist r = new RDlist();
                    decimal d = 0M;
                    r.autoid = dr["Autoid"].ToString().Trim();
                    r.ccode = dr["Ccode"].ToString().Trim();
                    r.ddate = dr["Ddate"].ToString().Replace("0:00:00", "").Trim();
                    r.cvencode = dr["Cvencode"].ToString().Trim();
                    r.cvenname = dr["Cvenname"].ToString().Trim();
                    r.cinvcode = dr["cinvcode"].ToString().Trim();
                    r.cinvname = dr["Cinvname"].ToString().Trim();
                    r.cinvm_unit = dr["Cinvm_unit"].ToString().Trim();
                    r.cwhcode = dr["cwhcode"].ToString().Trim();
                    r.cwhname = dr["cwhname"].ToString().Trim();
                    r.cpersoncode = dr["cpersoncode"].ToString().Trim();
                    r.fph = "";

                    //数量
                    r.iquantity = dr["Iquantity"].ToString().Trim() == "" ? "0" : dr["Iquantity"].ToString().Trim();
                    decimal dIquantity = decimal.Parse(r.iquantity);
                    r.iquantity = (decimal.Round(dIquantity, 6)).ToString();

                    //无税单价
                    decimal dzkq_price = GetPrice_Djfb(dr["cinvcode"].ToString().Trim(), dr["Cvencode"].ToString().Trim(), strYear, strFactory, r.ddate, r.iquantity);
                    r.zkq_price = dzkq_price.ToString().Trim();

                    //扣率
                    r.kl = dr["Kl"].ToString().Trim() == "" ? "1" : dr["Kl"].ToString().Trim();
                    decimal dKl = decimal.Parse(r.kl);
                    //结算单价
                    r.price = (dzkq_price * dKl).ToString();

                    //税率
                    r.itaxrate = dr["itaxrate"].ToString().Trim() == "" ? "0" : dr["itaxrate"].ToString().Trim();
                    decimal dItaxrate = decimal.Parse(r.itaxrate);
                    r.itaxrate = (decimal.Round(dItaxrate, 2)).ToString();

                    //1+税率=1+13/100
                    decimal dShuie = dItaxrate / 100;
                    decimal dShui = 1 + dShuie;

                    //含税单价 = 无税单价×（1+税率/100）
                    d = dzkq_price * dShui;
                    r.price_hs = (decimal.Round(d, 6)).ToString();

                    //税额 = 无税单价×数量×税率/100
                    d = dzkq_price * dIquantity * dShuie;
                    r.shuie = (decimal.Round(d, 2)).ToString();

                    //价税合计 = 无税单价×数量×（1+税率/100）
                    d = dzkq_price * dIquantity * dShui;
                    r.ksqhsje = (decimal.Round(d, 2)).ToString();

                    //结算税额 = 无税单价×数量×税率/100*扣率
                    d = dzkq_price * dIquantity * dShuie * dKl;
                    r.shuie_js = (decimal.Round(d, 2)).ToString();

                    //结算税额[含税] = 无税单价×扣率×数量×（1+税率/100）
                    d = dzkq_price * dKl * dIquantity * dShui;
                    r.jsje_hs = (decimal.Round(d, 2)).ToString();

                    list.Add(r);
                }
            }
            //该处理会导致必须使用json处理
            string json = JsonConvert.SerializeObject(list);
            return json;
        }

        //获取价格--入库单发布
        public decimal GetPrice_Djfb(string strInvcode, string strVenCode, string strYear, string strFactory, string strDate, string strNum)
        {
            decimal dPrice = 0M;
            SqlConnection con = new SqlConnection();
            try
            {
                con = bc.Get_con(strYear, strFactory);
                SqlCommand cmd = new SqlCommand("dbo.FN_F_getPrice", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@invcode", SqlDbType.Text).Value = strInvcode;
                cmd.Parameters.Add("@vencode", SqlDbType.Text).Value = strVenCode;
                cmd.Parameters.Add("@ddate", SqlDbType.Text).Value = strDate;
                if (decimal.Parse(strNum) > 0)
                {
                    cmd.Parameters.Add("@qty", SqlDbType.Decimal).Value = 1;
                }
                else
                {
                    cmd.Parameters.Add("@qty", SqlDbType.Decimal).Value = -1;
                }
                cmd.Parameters.Add("@price", SqlDbType.Decimal).Direction = ParameterDirection.ReturnValue;

                con.Open();
                cmd.ExecuteNonQuery();
                string strPrice = cmd.Parameters["@price"].Value.ToString();

                strPrice = strPrice == "" ? "0" : strPrice;
                dPrice = decimal.Parse(strPrice);
                dPrice = decimal.Round(dPrice, 6);
                con.Close();
                cmd.Dispose();
            }
            catch
            {
                con.Close();
            }

            return dPrice;
        }

        //入库单发布
        /// <summary>
        /// 更新用友数据库中入库单的发布状态
        /// 输入： 入库单号，发票号
        /// 返回： （更新成功，返回true；更新失败，返回false）
        /// </summary>
        [XmlInclude(typeof(Ccode))]
        [WebMethod]
        [System.Web.Services.Protocols.SoapHeader("header")]//用户身份验证的soap头
        public bool UpdateFb(ArrayList list, string strFactory, string strYear)
        {
            if (this.IsValiToken())
            {
                int iMax = 0;
                string strCcode = "";
                ArrayList listWhere = new ArrayList();
                ArrayList listSql = new ArrayList();
                foreach (Ccode c in list)
                {
                    //防止字符串溢出
                    if (strCcode.Length < 2500)
                    {
                        strCcode += "'" + c.cCode + "',";
                    }
                    else
                    {
                        listWhere.Add(strCcode.Substring(0, strCcode.Length - 1));
                        strCcode = "'" + c.cCode + "',";
                    }
                    iMax++;
                    if (iMax == list.Count)
                    {
                        listWhere.Add(strCcode.Substring(0, strCcode.Length - 1));
                    }
                }

                foreach (string strWhere in listWhere)
                {
                    listSql.Add("update rdrecords01 set Cdefine22 = 'Y' where AUTOID in (" + strWhere + ")");
                }
                if (bc.ExecListSql(listSql, strYear, strFactory))
                {
                    return true;
                }
            }
            return false;
        }

        //入库单取消发布
        /// <summary>
        /// 更新用友数据库中入库单的发布状态
        /// 输入： 入库单号，发票号
        /// 返回： （更新成功，返回true；更新失败，返回false）
        /// </summary>
        [XmlInclude(typeof(Ccode))]
        [WebMethod]
        [System.Web.Services.Protocols.SoapHeader("header")]//用户身份验证的soap头
        public bool UpdateFb_qx(ArrayList list, string strFactory, string strYear)
        {
            if (this.IsValiToken())
            {
                int iMax = 0;
                string strCcode = "";
                ArrayList listWhere = new ArrayList();
                ArrayList listSql = new ArrayList();
                foreach (Ccode c in list)
                {
                    //防止字符串溢出
                    if (strCcode.Length < 2500)
                    {
                        strCcode += "'" + c.cCode + "',";
                    }
                    else
                    {
                        listWhere.Add(strCcode.Substring(0, strCcode.Length - 1));
                        strCcode = "'" + c.cCode + "',";
                    }
                    iMax++;
                    if (iMax == list.Count)
                    {
                        listWhere.Add(strCcode.Substring(0, strCcode.Length - 1));
                    }
                }

                foreach (string strWhere in listWhere)
                {
                    listSql.Add("update rdrecords01 set Cdefine22 = NULL where AUTOID in (" + strWhere + ")");
                }
                if (bc.ExecListSql(listSql, strYear, strFactory))
                {
                    return true;
                }
            }
            return false;
        }

        //绑票
        /// <summary>
        /// 更新用友数据库中入库单的发票号
        /// 输入： 入库单号，发票号
        /// 返回： （更新成功，返回true；更新失败，返回false）
        /// </summary>
        [XmlInclude(typeof(Ccode))]
        [WebMethod]
        [System.Web.Services.Protocols.SoapHeader("header")]//用户身份验证的soap头
        public bool UpdateFph(ArrayList list, string strFph, string strFactory, string strYear)
        {
            if (this.IsValiToken())
            {
                int iMax = 0;
                string strCcode = "";
                ArrayList listWhere = new ArrayList();
                ArrayList listSql = new ArrayList();
                foreach (Ccode c in list)
                {
                    //防止字符串溢出
                    if (strCcode.Length < 2500)
                    {
                        strCcode += "'" + c.cCode + "',";
                    }
                    else
                    {
                        listWhere.Add(strCcode.Substring(0, strCcode.Length - 1));
                        strCcode = "'" + c.cCode + "',";
                    }
                    iMax++;
                    if (iMax == list.Count)
                    {
                        listWhere.Add(strCcode.Substring(0, strCcode.Length - 1));
                    }
                }

                foreach (string strWhere in listWhere)
                {
                    listSql.Add("update rdrecords01 set Cdefine23 = '" + strFph.Trim() + "' where AUTOID in (" + strWhere + ")");
                }
                if (bc.ExecListSql(listSql, strYear, strFactory))
                {
                    return true;
                }
            }
            return false;
        }

        //取消绑票
        /// <summary>
        /// 更新用友数据库中入库单的发票号
        /// 输入： AUTOID
        /// 返回： （更新成功，返回true；更新失败，返回false）
        /// </summary>
        [XmlInclude(typeof(Ccode))]
        [WebMethod]
        [System.Web.Services.Protocols.SoapHeader("header")]//用户身份验证的soap头
        public bool UnsetFph(string strFph, string strVencode, string strFactory, string strYear)
        {
            if (this.IsValiToken())
            {
                string strSql = "update rdrecords01 set cDefine23 = null where cDefine23 = '" + strFph + "' and chvencode = '" + strVencode + "' ";
                if (bc.ExecSql(strSql, strYear, strFactory))
                {
                    return true;
                }
            }
            return false;
        }

        //获取入库单扣率
        /// <summary>
        /// 查询用友数据库中入库单扣率明细
        /// 输入： 发票号,供应商编号
        /// 返回： list
        /// </summary>
        [XmlInclude(typeof(Kl))]
        [WebMethod]
        [System.Web.Services.Protocols.SoapHeader("header")]//用户身份验证的soap头
        public string GetKl(ArrayList list, string strFactory, string strYear)
        {
            List<Kl> Kllist = new List<Kl>();
            if (this.IsValiToken())
            {
                int iMax = 0;
                string strCcode = "";
                string strSql = "";
                DataSet ds = new DataSet();
                ArrayList listWhere = new ArrayList();
                ArrayList listSql = new ArrayList();
                foreach (Kl c in list)
                {
                    //防止字符串溢出
                    if (strCcode.Length < 2500)
                    {
                        strCcode += "'" + c.ccode + "',";
                    }
                    else
                    {
                        listWhere.Add(strCcode.Substring(0, strCcode.Length - 1));
                        strCcode = "'" + c.ccode + "',";
                    }
                    iMax++;
                    if (iMax == list.Count)
                    {
                        listWhere.Add(strCcode.Substring(0, strCcode.Length - 1));
                    }
                }
                foreach (string strWhere in listWhere)
                {
                    strSql = "select autoid,isnull(Kl,1) as Kl from FN_V_RDList where ccode in (" + strWhere + ")";
                    DataTable dt = bc.SearchTableSql(strSql, strYear, strFactory);
                    foreach (DataRow dr in dt.Rows)
                    {
                        Kl r = new Kl();
                        r.autoid = dr["autoid"].ToString().Trim();
                        r.kl = dr["Kl"].ToString().Trim();
                        Kllist.Add(r);
                    }
                }
            }
            //该处理会导致必须使用json处理
            string json = JsonConvert.SerializeObject(Kllist);
            return json;
        }

        //取消审核时，检查入库单Dsdate是否未空
        /// <summary>
        /// 输入： 入库单
        /// 返回： 
        /// </summary>
        [WebMethod]
        [System.Web.Services.Protocols.SoapHeader("header")]//用户身份验证的soap头
        public bool CheckDsdate(ArrayList list, string strFactory, string strYear)
        {
            if (this.IsValiToken())
            {
                int iMax = 0;
                string strCcode = "";
                string strSql = "";
                DataSet ds = new DataSet();
                ArrayList listWhere = new ArrayList();
                ArrayList listSql = new ArrayList();
                try
                {
                    foreach (Ccode c in list)
                    {
                        //防止字符串溢出
                        if (strCcode.Length < 2500)
                        {
                            strCcode += "'" + c.cCode + "',";
                        }
                        else
                        {
                            listWhere.Add(strCcode.Substring(0, strCcode.Length - 1));
                            strCcode = "'" + c.cCode + "',";
                        }
                        iMax++;
                        if (iMax == list.Count)
                        {
                            listWhere.Add(strCcode.Substring(0, strCcode.Length - 1));
                        }
                    }

                    foreach (string strWhere in listWhere)
                    {
                        strSql = "select count(*) from Rdrecords01 where  autoid in (  select autoid from FN_V_RDList where ccode in (" + strWhere + ")) and dsdate is null;";
                        string strReturn = bc.SearchOneSql(strSql, strYear, strFactory);
                        if (strReturn.Trim() == "0")
                        {
                            return false;
                        }
                    }
                    return true;
                }
                catch (Exception e)
                {
                    return false;
                }
            }
            return false;
        }

        //获取采购订单
        /// <summary>
        /// 查询用友数据库中采购订单
        /// 输入： 业务员账号，日期范围，供应商账号，采购单号
        /// 返回： list
        /// </summary>
        [XmlInclude(typeof(POlist))]
        [WebMethod]
        [System.Web.Services.Protocols.SoapHeader("header")]//用户身份验证的soap头
        public string GetPoList(string strPersoncode, string strBdate, string strEdate, string strVencode, string strCpoid, string strFactory, string strYear)
        {
            List<POlist> list = new List<POlist>();
            if (this.IsValiToken())
            {
                string strSql = "SELECT M.CPOID  ,d.id as Detailsid ,M.DPODATE ,M.CVENCODE ,v.cvenname ,M.ITAXRATE ,M.CPERSONCODE ,D.CINVCODE ,I.CINVNAME ,"
                              + "  D.IQUANTITY ,D.dArriveDate FROM PO_POMAIN M JOIN PO_Podetails  D ON  M.poID=D.poID join vendor v on v.cvencode=m.cvencode "
                              + "  LEFT JOIN Inventory I ON D.cInvCode=I.CINVCODE   "
                              + " where Dpodate >= cast('" + strBdate.Trim() + "' as datetime) AND Dpodate <= cast('" + strEdate.Trim() + "' as datetime)  and "
                              + " ( M.CDEFINE14 is null or M.CDEFINE14 = '')";
                if (strPersoncode != "")
                {
                    string strUserCodeBd = GetUserbd(strPersoncode);
                    if (strUserCodeBd == "")
                    {
                        strSql += " and M.CPERSONCODE = '" + strPersoncode.Trim() + "' ";
                    }
                    else
                    {
                        strSql += " and M.CPERSONCODE in (" + strUserCodeBd.Trim() + ") ";
                    }
                }
                if (strVencode != "")
                {
                    strSql += " and M.CVENCODE = '" + strVencode.Trim() + "' ";
                }
                if (strCpoid != "")
                {
                    strSql += " and M.CPOID like '%" + strCpoid.Trim() + "%' ";
                }
                strSql += " order by CPOID,  Detailsid  ";
                DataTable dt = bc.SearchTableSql(strSql, strYear.Trim(), strFactory.Trim());
                foreach (DataRow dr in dt.Rows)
                {
                    decimal d = 0M;
                    POlist r = new POlist();
                    r.cCpoid = dr["CPOID"].ToString().Trim();
                    r.cDarriveDate = dr["dArriveDate"].ToString().Replace("0:00:00", "").Trim();
                    r.cDpodate = dr["DPODATE"].ToString().Replace("0:00:00", "").Trim();
                    r.cInvcode = dr["CINVCODE"].ToString().Trim();
                    r.cInvname = dr["CINVNAME"].ToString().Trim();
                    r.cPersoncode = dr["CPERSONCODE"].ToString().Trim();
                    r.cIquantity = dr["IQUANTITY"].ToString().Trim() == "" ? "0" : dr["IQUANTITY"].ToString().Trim();
                    d = decimal.Parse(r.cIquantity);
                    r.cIquantity = (decimal.Round(d, 4)).ToString();
                    r.iTaxrate = dr["ITAXRATE"].ToString().Trim();
                    r.cVencode = dr["CVENCODE"].ToString().Trim();
                    r.cVenname = dr["cvenname"].ToString().Trim();
                    r.cDetailsid = dr["Detailsid"].ToString().Trim();
                    list.Add(r);
                }
            }
            //该处理会导致必须使用json处理
            string json = JsonConvert.SerializeObject(list);
            return json;
        }

        //获取采购订单入库情况表
        /// <summary>
        /// 查询用友数据库中采购订单入库情况表
        /// 输入： 采购单项次，工厂，年度
        /// 返回： list
        /// </summary>
        [XmlInclude(typeof(PoRklist))]
        [WebMethod]
        [System.Web.Services.Protocols.SoapHeader("header")]//用户身份验证的soap头
        public string GetPoRkList(string strID, string strFactory, string strYear)
        {
            List<PoRklist> list = new List<PoRklist>();
            if (this.IsValiToken())
            {
                string strSql = "select * from CFKF_V_CGDD_DHD_RKD where ID = '" + strID + "' ";
                strSql += " ORDER BY CCODE;  ";
                DataTable dt = bc.SearchTableSql(strSql, strYear.Trim(), strFactory.Trim());
                foreach (DataRow dr in dt.Rows)
                {
                    decimal d = 0M;
                    PoRklist r = new PoRklist();
                    r.cCpoid = dr["CPOID"].ToString().Trim();
                    r.dPODate = dr["dPODate"].ToString().Replace("0:00:00", "").Trim();
                    r.cInvcode = dr["cInvCode"].ToString().Trim();
                    r.cInvname = dr["cInvName"].ToString().Trim();
                    r.iQuantity = dr["iQuantity"].ToString().Trim() == "" ? "0" : dr["iQuantity"].ToString().Trim();
                    d = decimal.Parse(r.iQuantity);
                    r.iQuantity = (decimal.Round(d, 4)).ToString();
                    r.cCode = dr["cCode"].ToString().Trim();
                    r.dDate = dr["dDate"].ToString().Replace("0:00:00", "").Trim();
                    r.dhsl = dr["dhsl"].ToString().Trim() == "" ? "0" : dr["dhsl"].ToString().Trim();
                    d = decimal.Parse(r.dhsl);
                    r.dhsl = (decimal.Round(d, 4)).ToString();
                    r.rkdh = dr["rkdh"].ToString().Trim();
                    r.rkrq = dr["rkrq"].ToString().Replace("0:00:00", "").Trim();
                    r.rksl = dr["rksl"].ToString().Trim() == "" ? "0" : dr["rksl"].ToString().Trim();
                    d = decimal.Parse(r.rksl);
                    r.rksl = (decimal.Round(d, 4)).ToString();
                    r.cInvStd = dr["cInvStd"].ToString().Trim();
                    r.cComUnitName = dr["cComUnitName"].ToString().Trim();
                    list.Add(r);
                }
            }
            //该处理会导致必须使用json处理
            string json = JsonConvert.SerializeObject(list);
            return json;
        }

        //获取采购订单入库情况表--多项次
        /// <summary>
        /// 查询用友数据库中采购订单入库情况表
        /// 输入： 采购单项次，工厂，年度
        /// 返回： list
        /// </summary>
        [XmlInclude(typeof(PoRklist))]
        [WebMethod]
        [System.Web.Services.Protocols.SoapHeader("header")]//用户身份验证的soap头
        public string GetPoRkLists(ArrayList listId, string strFactory, string strYear)
        {
            List<PoRklist> list = new List<PoRklist>();
            if (this.IsValiToken())
            {
                int iMax = 0;
                string strCcode = "";
                ArrayList listWhere = new ArrayList();
                ArrayList listSql = new ArrayList();
                foreach (Ccode c in listId)
                {
                    //防止字符串溢出
                    if (strCcode.Length < 2500)
                    {
                        strCcode += "'" + c.cCode + "',";
                    }
                    else
                    {
                        listWhere.Add(strCcode.Substring(0, strCcode.Length - 1));
                        strCcode = "'" + c.cCode + "',";
                    }
                    iMax++;
                    if (iMax == listId.Count)
                    {
                        listWhere.Add(strCcode.Substring(0, strCcode.Length - 1));
                    }
                }

                foreach (string strID in listWhere)
                {
                    string strSql = "select * from CFKF_V_CGDD_DHD_RKD where ID in (" + strID + ")  ORDER BY CCODE;";
                    DataTable dt = bc.SearchTableSql(strSql, strYear.Trim(), strFactory.Trim());
                    foreach (DataRow dr in dt.Rows)
                    {
                        decimal d = 0M;
                        PoRklist r = new PoRklist();
                        r.cCpoid = dr["CPOID"].ToString().Trim();
                        r.ID = dr["ID"].ToString().Trim();
                        r.dPODate = dr["dPODate"].ToString().Replace("0:00:00", "").Trim();
                        r.cInvcode = dr["cInvCode"].ToString().Trim();
                        r.cInvname = dr["cInvName"].ToString().Trim();
                        r.iQuantity = dr["iQuantity"].ToString().Trim() == "" ? "0" : dr["iQuantity"].ToString().Trim();
                        d = decimal.Parse(r.iQuantity);
                        r.iQuantity = (decimal.Round(d, 4)).ToString();
                        r.cCode = dr["cCode"].ToString().Trim();
                        r.dDate = dr["dDate"].ToString().Replace("0:00:00", "").Trim();
                        r.dhsl = dr["dhsl"].ToString().Trim() == "" ? "0" : dr["dhsl"].ToString().Trim();
                        d = decimal.Parse(r.dhsl);
                        r.dhsl = (decimal.Round(d, 4)).ToString();
                        r.rkdh = dr["rkdh"].ToString().Trim();
                        r.rkrq = dr["rkrq"].ToString().Replace("0:00:00", "").Trim();
                        r.rksl = dr["rksl"].ToString().Trim() == "" ? "0" : dr["rksl"].ToString().Trim();
                        d = decimal.Parse(r.rksl);
                        r.rksl = (decimal.Round(d, 4)).ToString();
                        r.cInvStd = dr["cInvStd"].ToString().Trim();
                        r.cComUnitName = dr["cComUnitName"].ToString().Trim();
                        list.Add(r);
                    }
                }
            }
            //该处理会导致必须使用json处理
            string json = JsonConvert.SerializeObject(list);
            return json;
        }

        //发布后反写采购单表
        /// <summary>
        /// 更新用友数据库中采购单的发布状态
        /// 输入： 入库单号，发票号
        /// 返回： （更新成功，返回true；更新失败，返回false）
        /// </summary>
        [XmlInclude(typeof(Ccode))]
        [WebMethod]
        [System.Web.Services.Protocols.SoapHeader("header")]//用户身份验证的soap头
        public bool UpdatePoListFb(ArrayList list, string strFactory, string strYear)
        {
            if (this.IsValiToken())
            {
                int iMax = 0;
                string strCcode = "";
                ArrayList listWhere = new ArrayList();
                ArrayList listSql = new ArrayList();
                foreach (Ccode c in list)
                {
                    //防止字符串溢出
                    if (strCcode.Length < 2500)
                    {
                        strCcode += "'" + c.cCode + "',";
                    }
                    else
                    {
                        listWhere.Add(strCcode.Substring(0, strCcode.Length - 1));
                        strCcode = "'" + c.cCode + "',";
                    }
                    iMax++;
                    if (iMax == list.Count)
                    {
                        listWhere.Add(strCcode.Substring(0, strCcode.Length - 1));
                    }
                }

                foreach (string strWhere in listWhere)
                {
                    listSql.Add("update PO_POMAIN set CDEFINE14 = 'Y' where CPOID in (" + strWhere + ")");
                }
                if (bc.ExecListSql(listSql, strYear, strFactory))
                {
                    return true;
                }
            }
            return false;
        }

        //取消发布后反写采购单表
        /// <summary>
        /// 更新用友数据库中采购单的发布状态
        /// 输入： 入库单号，发票号
        /// 返回： （更新成功，返回true；更新失败，返回false）
        /// </summary>
        [XmlInclude(typeof(Ccode))]
        [WebMethod]
        [System.Web.Services.Protocols.SoapHeader("header")]//用户身份验证的soap头
        public bool UpdatePoListFb_qx(ArrayList list, string strFactory, string strYear)
        {
            if (this.IsValiToken())
            {
                int iMax = 0;
                string strCcode = "";
                ArrayList listWhere = new ArrayList();
                ArrayList listSql = new ArrayList();
                SqlConnection con = new SqlConnection();
                if (con.ConnectionString == "")
                {
                    con = bc.Get_con(strYear, strFactory);
                }
                foreach (Ccode c in list)
                {
                    //防止字符串溢出
                    if (strCcode.Length < 2500)
                    {
                        strCcode += "'" + c.cCode + "',";
                    }
                    else
                    {
                        listWhere.Add(strCcode.Substring(0, strCcode.Length - 1));
                        strCcode = "'" + c.cCode + "',";
                    }
                    iMax++;
                    if (iMax == list.Count)
                    {
                        listWhere.Add(strCcode.Substring(0, strCcode.Length - 1));
                    }
                }

                foreach (string strWhere in listWhere)
                {
                    listSql.Add("update PO_POMAIN set CDEFINE14 = NULL where CPOID in (" + strWhere + ")");
                }
                if (bc.ExecListSql(listSql, strYear, strFactory))
                {
                    return true;
                }
            }
            return false;
        }

        //获取供应商
        /// <summary>
        /// 查询用友数据库中供应商表
        /// 返回： list
        /// </summary>
        [WebMethod]
        [System.Web.Services.Protocols.SoapHeader("header")]//用户身份验证的soap头
        public string GetGysList(string strFactory, string strYear)
        {
            List<Vendor> list = new List<Vendor>();
            if (this.IsValiToken())
            {
                string strSql = "select cVenCode,cVenName from VENDOR where cVCCode>=600 and cVCCode<700 order by cVenCode";
                DataTable dt = bc.SearchTableSql(strSql, strYear, strFactory);
                foreach (DataRow dr in dt.Rows)
                {
                    Vendor r = new Vendor();
                    r.cVenCode = dr[0].ToString().Trim();
                    r.cVenName = dr[1].ToString().Trim();
                    list.Add(r);
                }
                //该处理会导致必须使用json处理
            }
            string json = JsonConvert.SerializeObject(list);
            return json;
        }

        //获取价格
        /// <summary>
        /// 查询用友数据库中供应商表
        /// 返回： list
        /// </summary>
        [XmlInclude(typeof(Price))]
        [WebMethod]
        [System.Web.Services.Protocols.SoapHeader("header")]//用户身份验证的soap头
        public string GetPrice(ArrayList list, string strYear, string strFactory)
        {
            if (this.IsValiToken())
            {
                SqlConnection con = new SqlConnection();
                foreach (Price r in list)
                {
                    if (con.ConnectionString == "")
                    {
                        con = bc.Get_con(strYear, strFactory);
                    }
                    SqlCommand cmd = new SqlCommand("dbo.FN_F_getPrice", con);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("@invcode", SqlDbType.Text).Value = r.cInvcode;
                    cmd.Parameters.Add("@vencode", SqlDbType.Text).Value = r.cVenCode;
                    cmd.Parameters.Add("@ddate", SqlDbType.Text).Value = r.cDdate;
                    cmd.Parameters.Add("@qty", SqlDbType.Decimal).Value = (r.iquantity == "" ? 0 : float.Parse(r.iquantity));
                    cmd.Parameters.Add("@price", SqlDbType.Decimal).Direction = ParameterDirection.ReturnValue;

                    con.Open();
                    cmd.ExecuteNonQuery();
                    string strPrice = cmd.Parameters["@price"].Value.ToString();

                    r.cPrice = strPrice == "" ? "0" : strPrice;
                    con.Close();
                    cmd.Dispose();
                }
            }
            //该处理会导致必须使用json处理
            string json = JsonConvert.SerializeObject(list);
            return json;
        }

        //获取用户对照
        private string GetUserbd(string strUserCode)
        {
            string strReturn = "";
            string strSql = "select cUserCodeOld from [G_UserM_dz] where [cUserCode] = '" + strUserCode + "'";
            string UserCodeOld = bc.SearchOneSql(strSql);
            if (UserCodeOld.Length > 0)
            {
                string[] strs = UserCodeOld.Split(',');
                foreach (string str in strs)
                {
                    if (str != "")
                        strReturn += "'" + str + "',";
                }
                strReturn = strReturn.Substring(0, strReturn.Length - 1);
            }
            return strReturn;
        }

        /// <summary>
        /// 安全验证令牌
        /// </summary>
        public class SecurityToken : SoapHeader
        {
            //安全密钥
            public string Key { get; set; }
        }

        //物料价格
        [Serializable]
        public class Price
        {
            //行项次
            public string autoid { get; set; }
            //物料号
            public string cInvcode { get; set; }
            //供应商编号
            public string cVenCode { get; set; }
            //日期
            public string cDdate { get; set; }
            //数量
            public string iquantity { get; set; }
            //不含税单价
            public string cPrice { get; set; }
        }

        //扣率
        [Serializable]
        public class Kl
        {
            //入库单号
            public string ccode { get; set; }
            //项次
            public string autoid { get; set; }
            //扣率
            public string kl { get; set; }
        }

        //单号
        [Serializable]
        public class Ccode
        {
            //单号
            public string cCode { get; set; }
        }

        //供应商
        public class Vendor
        {
            //供应商编码
            public string cVenCode { get; set; }
            //供应商名称
            public string cVenName { get; set; }
        }

        //入库单
        public class RDlist
        {
            //入库单号
            public string ccode { get; set; }
            //项次
            public string autoid { get; set; }
            //日期
            public string ddate { get; set; }
            //供应商编码
            public string cvencode { get; set; }
            //供应商名称
            public string cvenname { get; set; }
            //物料编码
            public string cinvcode { get; set; }
            //物料名称
            public string cinvname { get; set; }
            //数量
            public string iquantity { get; set; }
            //计量单位
            public string cinvm_unit { get; set; }
            //无税单价
            public string zkq_price { get; set; }
            //税率
            public string itaxrate { get; set; }
            //含税单价
            public string price_hs { get; set; }
            //税额
            public string shuie { get; set; }
            //价税合计
            public string ksqhsje { get; set; }
            //扣率
            public string kl { get; set; }
            //结算单价
            public string price { get; set; }
            //结算税额
            public string shuie_js { get; set; }
            //结算金额[含税]
            public string jsje_hs { get; set; }
            //发票号
            public string fph { get; set; }
            //仓库编码
            public string cwhcode { get; set; }
            //仓库名称
            public string cwhname { get; set; }
            //业务员编码
            public string cpersoncode { get; set; }
        }

        //采购单
        public class POlist
        {
            //采购单号
            public string cCpoid { get; set; }
            //项次
            public string cDetailsid { get; set; }
            //订单日期
            public string cDpodate { get; set; }
            //供应商编码
            public string cVencode { get; set; }
            //供应商名称
            public string cVenname { get; set; }
            //税率
            public string iTaxrate { get; set; }
            //业务员编码
            public string cPersoncode { get; set; }
            //物料编码
            public string cInvcode { get; set; }
            //物料名称
            public string cInvname { get; set; }
            //数量
            public string cIquantity { get; set; }
            //预计到达日期
            public string cDarriveDate { get; set; }
        }

        //采购单入库情况表
        public class PoRklist
        {
            //采购单号
            public string cCpoid { get; set; }
            //项次
            public string ID { get; set; }
            //采购订单日期
            public string dPODate { get; set; }
            //物料编码
            public string cInvcode { get; set; }
            //物料名称
            public string cInvname { get; set; }
            //规格
            public string cInvStd { get; set; }
            //采购单数量
            public string iQuantity { get; set; }
            //计量单位
            public string cComUnitName { get; set; }
            //到货单号
            public string cCode { get; set; }
            //到货日期
            public string dDate { get; set; }
            //到货数量
            public string dhsl { get; set; }
            //入库单号
            public string rkdh { get; set; }
            //入库日期
            public string rkrq { get; set; }
            //入库数量
            public string rksl { get; set; }
        }

        public class MySoapHeader : SoapHeader
        {
            //账号
            public string UserName { get; set; }
            //密码
            public string PassWord { get; set; }
        }

    }
}
