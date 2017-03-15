-- require("hewei_test.test.lua")
-- require("hewei_test.test")

print("inner test.lua is detected!!!!!")

local logStr = "0 "
for i = 2, 9, 2 do
	logStr = logStr .. i .. " "
end

print(logStr)
