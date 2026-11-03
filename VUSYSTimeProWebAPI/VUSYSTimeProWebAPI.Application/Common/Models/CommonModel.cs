using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace VUSYSTimeProWebAPI.Application.Common.Models
{
    public class CommonModel: RequestParameterCommonModel
    {   
        /// <summary>
        /// Status
        /// </summary>
        public string Status { get; set; }
    }

    public interface ICommonModel : IRequestParameterCommonModel
    {
        /// <summary>
        /// Status
        /// </summary>
        public string Status { get; set; }

    }

    public class RequestParameterCommonModel
    {
        /// <summary>
        /// Is Deleted
        /// </summary>
        [Display(Name = "Is Deleted")]
        public bool IsDeleted { get; set; }

        /// <summary>
        /// Created By
        /// </summary>
        [Display(Name = "Created By")]
        public int CreatedBy { get; set; }

        /// <summary>
        /// Updated By
        /// </summary>
        [Display(Name = "Updated By")]
        public int UpdatedBy { get; set; }
    }

    public interface IRequestParameterCommonModel
    {
        /// <summary>
        /// Is Deleted
        /// </summary>
        [Display(Name = "Is Deleted")]
        public bool IsDeleted { get; set; }

        /// <summary>
        /// Created By
        /// </summary>
        [Display(Name = "Created By")]
        public int CreatedBy { get; set; }

        /// <summary>
        /// Updated By
        /// </summary>
        [Display(Name = "Updated By")]
        public int UpdatedBy { get; set; }
    }

    public class CommonResponseMessageModel
    {
        /// <summary>
        /// Id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Message
        /// </summary>
        public string Message { get; set; }
    }

    public class PaginationCommonModel
    {
        /// <summary>
        /// Page Size
        /// </summary>
        [Display(Name = "Page Size")]
        public int PageSize { get; set; } = 0;

        /// <summary>
        /// Page Number
        /// </summary>
        [Display(Name = "Page Number")]
        public int PageNumber { get; set; } = 0;
    }

    public class PaginationCommonMandatoryModel
    {

        /// <summary>
        /// Page Size
        /// </summary>
        [Display(Name = "Page Size")]
        [Required(ErrorMessage = "Enter The Page Size.")]
        public int PageSize { get; set; } = 0;

        /// <summary>
        /// Page Number
        /// </summary>
        [Display(Name = "Page Number")]
        [Required(ErrorMessage = "Enter The Page Number.")]
        public int PageNumber { get; set; } = 0;
    }

    public class CommonColumnFilterRequestParam
    {
        /// <summary>
        /// Column Name
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Column Name")]
        public string ColumnName { get; set; }

        /// <summary>
        /// Search Keyword
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Search Keyword")]
        public string SearchKeyword { get; set; }

        /// <summary>
        /// Sort Order
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Sort Order")]
        public string SortOrder { get; set; }

        /// <summary>
        /// Column Sequence
        /// </summary>
        [Display(Name = "Column Sequence")]
        public int ColumnSequence { get; set; } = 0;
    }

    public class CommonPaginationListRequestParam : PaginationCommonMandatoryModel
    {
        /// <summary>
        /// Search Filter
        /// </summary>
        [Display(Name = "Search Filter")]
        public List<CommonColumnFilterRequestParam> SearchFilter { get; set; }

        /// <summary>
        /// Is Global Search
        /// </summary>
        [Display(Name = "Is Global Search")]
        public int IsGlobalSearch { get; set; } = 0;

        /// <summary>
        /// Sort Order
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Sort Order")]
        public string SortOrder { get; set; }

    }
}
