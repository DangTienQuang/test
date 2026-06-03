-- Create a default branch with ID 1
INSERT IGNORE INTO `Branches` (`BranchId`, `Name`, `Address`, `IsActive`) VALUES (1, 'Default Branch', 'Default Address', 1);

-- Update all existing bookings to point to the default branch
UPDATE `Bookings` SET `BranchId` = 1 WHERE `BranchId` = 0;
