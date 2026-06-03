using System;
using MySqlConnector;

class Program {
    static void Main() {
        var connStr = "Server=mysql-9cc1d6a-carsmartwash123.c.aivencloud.com;Port=10321;Database=defaultdb;Uid=avnadmin;Pwd=AVNS_ujCwAUs1iP7CeZb2vd7;SslMode=Required;";
        using var conn = new MySqlConnection(connStr);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SET FOREIGN_KEY_CHECKS = 0;
            DROP TABLE IF EXISTS `StaffLaneAssignments`;
            DROP TABLE IF EXISTS `EmployeeProfiles`;
            ALTER TABLE `BookingDetails` DROP FOREIGN KEY `FK_BookingDetails_Lanes_ProcessingLaneId`;
            ALTER TABLE `BookingDetails` DROP FOREIGN KEY `FK_BookingDetails_Users_ProcessingStaffId`;
            ALTER TABLE `BookingDetails` DROP INDEX `IX_BookingDetails_ProcessingLaneId`;
            ALTER TABLE `BookingDetails` DROP INDEX `IX_BookingDetails_ProcessingStaffId`;
            ALTER TABLE `BookingDetails` DROP COLUMN `ProcessingLaneId`;
            ALTER TABLE `BookingDetails` DROP COLUMN `ProcessingStaffId`;
            ALTER TABLE `Bookings` DROP FOREIGN KEY `FK_Bookings_Branches_BranchId`;
            ALTER TABLE `Bookings` DROP INDEX `IX_Bookings_BranchId`;
            ALTER TABLE `Bookings` DROP COLUMN `BranchId`;
            DROP TABLE IF EXISTS `Lanes`;
            DROP TABLE IF EXISTS `Branches`;
            DELETE FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260603115109_AddBranchAndLaneSystem';
            SET FOREIGN_KEY_CHECKS = 1;
        ";
        try { cmd.ExecuteNonQuery(); } catch (Exception e) { Console.WriteLine(e); }
    }
}
