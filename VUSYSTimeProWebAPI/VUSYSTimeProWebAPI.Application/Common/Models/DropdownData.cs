using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace VUSYSTimeProWebAPI.Application.Common.Models
{
	public class DropdownData
	{
		/// <summary>
		/// Source Table
		/// </summary>
		[DefaultValue("")]
		[Display(Name = "Source Table")]
		[Required(ErrorMessage = "Enter The Source Table (Department/OthersText/OthersNum/PartnerName/DivisionName/LocalGSTData/InterstateGSTData/Currency/FGCEDType/FGItemType/FGItemSeries/FGPriceCode/FGHSNCode/PlatingName/PortName/CustomerPartnerName/CountryName/DocumentsName/FrieghtName/PriceScopeName/ApprovedBy/Dimension/BOMItemType/ToolLocationVendorList/PreparedBy/SystemID/DistributorSelection/PlaceOfSupply/DeliveryChallanTo/DeliveryNoteHSNCode/CharacteristicMaster/InspectionItems/CalibrationAgencyCode/PackingInfoItemCode/OAReference/ScheduleTypeText).")]
		public string SourceTable { get; set; }

		/// <summary>
		/// Argument Value
		/// </summary>
		[DefaultValue("")]
		[Display(Name = "Argument Value")]
		public string ArgumentValue { get; set; }
	}
}
