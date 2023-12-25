import socket
from queue import Queue
from threading import Thread, current_thread
import numpy as np
import matplotlib.pyplot as plt

plt.ion()
plt.figure(1)
t_list = []
result_list = []


class Data:
    def __init__(self):
        self.key = 0
        self.frame = 0
        self.value = 0

def start_socket_loop(args,name):
    print(args)
    self = args
    self.socket_server = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    self.socket_server.bind(("10.10.20.93", 9099))
    self.socket_server.listen(1)
    result = self.socket_server.accept()
    conn = result[0]
    address = result[1]
    print(conn)
    print(address)
    self.frame_dic.clear()
    while True:
        data = conn.recv(1024)
        if not data:
            break
        print(data)
        key = int.from_bytes(bytes[0:4], byteorder='little', signed=True)
        frame = int.from_bytes(bytes[4:8], byteorder='little', signed=True)
        value = int.from_bytes(bytes[8:12], byteorder='little', signed=True)
        data = Data()
        data.key = key
        data.frame = frame
        data.value = value
        self.add_frame_data(data)
class Server:
    def __init__(self):
        self.frame_dic = dict()
        self.frames = []

    def start(self):
        self.socket_thread = Thread(target=start_socket_loop, args = (self,0) ,name = "socket_thread",daemon=True)
        # 设置守护线程【可选】
        # self.socket_thread.setDaemon(True)
        # 启动线程
        self.socket_thread.start()


    def add_frame_data(self,data):
        for i in range(len(self.frames) - 1, -1, -1):
            if self.frames[i] < data.frame:
                self.frames.insert(i,data.frame)
                break


    def draw(self):
        while True :
            pass
            # print(len(self.frame_dic))
            # keys = []
            # for key in self.frame_dic.keys():
            #     keys.append(key)


    def close(self):
        self.socket_server.close()

if __name__ == '__main__':
     socket_server = Server()
     socket_server.start()
     socket_server.draw()
     #
     # bytes = b'\x01\x00\x00\x00\xf7\x00\x00\x00\x1e\x00\x00\x00'
     #
     # i1 = int.from_bytes(bytes[0:4], byteorder='little', signed=True)
     # i2 = int.from_bytes(bytes[4:8], byteorder='little', signed=True)
     # i3 = int.from_bytes(bytes[8:12], byteorder='little', signed=True)
     #
     # print(i1)
     # print(i2)
     # print(i3)