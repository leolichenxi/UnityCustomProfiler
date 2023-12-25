import os

def scan_devices():
    "获取当前连接的设备"
    result = os.popen("adb devices").read()
    print(result)
    devices = result.split("\n")[1:]
    targets = []
    for device in devices:
        if device.strip():
            tmp = device.split()
            if len(tmp) == 2 and tmp[1] == 'device':
                deviceInfo = {}
                deviceID = tmp[0]
                deviceInfo['id'] = deviceID
                deviceInfo["os"] = os.popen(
                    "adb -s %s shell getprop ro.build.version.release" % (deviceID)).read().strip()
                deviceInfo["name"] = os.popen(
                    "adb -s %s shell getprop ro.product.model" % (deviceID)).read().strip().replace(" ", "-")
                targets.append(deviceInfo)
    return targets