using CommunityToolkit.Mvvm.ComponentModel;
using MediatR;
using OrderHub.Domain.Enums;
using OrderHub.UI.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.Queries.WhatsappGroupQueries;

namespace OrderHub.UI.Features.WhatsappGroups.Editor;

public abstract partial class ViewModel : EditorViewModelBase
{
    protected readonly IMediator _mediator;

    public ViewModel(IMediator mediator)
    {
        _notifyPropertiesNames = [nameof(Name), nameof(GroupType)];
        _mediator = mediator;
        ValidateAllProperties();
    }

    [ObservableProperty]
    [Required(ErrorMessage = "يجب ادخال اسم الجروب")]
    [NotifyDataErrorInfo]
    [Display(Name = "اسم الجروب")]
    private string _name;

    [ObservableProperty]
    [Required(ErrorMessage = "يجب اختيار نوع الجروب")]
    [NotifyDataErrorInfo]
    [Display(Name = "نوع الجروب")]
    private EnumItem<WhatsappGroupType> _groupType;

    public IEnumerable<EnumItem<WhatsappGroupType>> GroupTypes =>
        Enum.GetValues<WhatsappGroupType>()
            .Cast<WhatsappGroupType>()
            .Select(e => new EnumItem<WhatsappGroupType>(e, e.GetDescription()));

    protected async Task<bool> SearchForExistedGroupd(string groupName)
    {
        return await _mediator.Send(new IsWhatsappGroupExistsQuery(groupName, CancellationToken.None));
    }
}
