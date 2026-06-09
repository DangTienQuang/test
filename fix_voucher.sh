#!/bin/bash
awk '
/private Task SendVoucherEmailAsync/ {
    print
    getline
    print
    getline
    print
    if ($0 ~ /^[ \t]*if \(string.IsNullOrWhiteSpace\(user.Email\)\) return;[ \t]*$/) {
        sub(/return;/, "return Task.CompletedTask;")
    }
    print
    next
}
{print}
' ./BLL/Services/VoucherCampaignService.cs > temp.cs && mv temp.cs ./BLL/Services/VoucherCampaignService.cs
