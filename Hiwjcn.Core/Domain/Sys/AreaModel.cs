﻿using Model;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebLogic.Model.Sys
{
    /// <summary>
    /// 全国省市区
    /// </summary>
    [Table("wp_area")]
    public class AreaModel : BaseEntity
    {
        /// <summary>
        /// 主键
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("primary_id")]
        public virtual int PrimaryID { get; set; }

        /// <summary>
        /// 地区编号
        /// </summary>
        [Column("area_id")]
        public virtual string AreaID { get; set; }

        /// <summary>
        /// 父级地区编号
        /// </summary>
        [Column("parent_id")]
        public virtual string ParentID { get; set; }

        /// <summary>
        /// 地区名称
        /// </summary>
        [Column("area_name")]
        public virtual string AreaName { get; set; }

        /// <summary>
        /// 地区级别
        /// </summary>
        [Column("area_level")]
        public virtual int AreaLevel { get; set; }

        /// <summary>
        /// 地区排序
        /// </summary>
        [Column("order_num")]
        public virtual int OrderNum { get; set; }
    }
}
