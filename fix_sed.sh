#!/bin/bash
awk '
/emailHtml/ {
    print
    getline
    if ($0 ~ /^[ \t]*\);[ \t]*$/) {
        sub(/\);/, "));")
    }
    print
    next
}
{print}
' ./BLL/Services/BookingService.cs > temp.cs && mv temp.cs ./BLL/Services/BookingService.cs
