cat ./BLL/Services/ManagerService.cs | awk '
/var lane = new Lane/ {
    print
    getline
    print
    getline
    print
    getline
    print
    print "                IsBusinessLane = request.IsBusinessLane,"
    getline
    print
    next
}
1
' > ./BLL/Services/ManagerService.cs.tmp
mv ./BLL/Services/ManagerService.cs.tmp ./BLL/Services/ManagerService.cs
