clc
clear
close all

% load("Messdaten_01-06-2022 13-08.mat");
load('Calib.mat')

% X = Y
% Y = Z
% Z = X

%% Plot Koordinatensystem

% plot3( [0  0  0; 0  0  0; 0  0  1], [0  0  0; 0  1  0; 0  0  0], [1  0  0; 0  0  0; 0  0  0], 'r',)

ex0 = [1 0 0]';
ey0 = [0 1 0]';
ez0 = [0 0 1]';
origin = [0,0,0];

v = [data.Velx(:,1) , data.Vely(:,1) , data.Velz(:,1)];
a = [data.Accx(:,1) , data.Accy(:,1) , data.Accz(:,1)];
v_angles = zeros(size(v));
v_len = zeros(size(v,1),1);
for i = 1 : length(v)
    v_angles(i,:) = getVelAngles(v(i,:)');
    v_len(i,1) = norm(v(i,:)');
%     v(i,:) = v(i,:)./v_len;
end

figure
plot(v)
legend('x','y','z')
figure
plot(v_len)
% return


for i = 1 : 10 :length(data.t)
    q = quaternion(data.Qw(i,1),data.Qx(i,1),data.Qy(i,1),data.Qz(i,1));
    
    
    
%     q.normalize();
    R_imu = quat2rotm(q);
    R = eye(3);
%     e = quat2eul(q);
%     R = eul2rotm(e);
    ex_board = (R * ex0);
    ey_board = (R * ey0);
    ez_board = (R * ez0);
    
    ex_imu = (R_imu * ex0);
    ey_imu = (R_imu * ey0);
    ez_imu = (R_imu * ez0);
    
    figure(1);
    plot3([origin(1) ex_board(1)],[origin(2) ex_board(2)],[origin(3) ex_board(3)],'k-^', 'LineWidth',1);
    hold on;
    plot3([origin(1) ey_board(1)],[origin(2) ey_board(2)],[origin(3) ey_board(3)],'k-^', 'LineWidth',1);
    plot3([origin(1) ez_board(1)],[origin(2) ez_board(2)],[origin(3) ez_board(3)],'k-^', 'LineWidth',1);
    
    plot3([origin(1) ex_imu(1)],[origin(2) ex_imu(2)],[origin(3) ex_imu(3)],'r-^', 'LineWidth',3);
    plot3([origin(1) ey_imu(1)],[origin(2) ey_imu(2)],[origin(3) ey_imu(3)],'g-^', 'LineWidth',3);
    plot3([origin(1) ez_imu(1)],[origin(2) ez_imu(2)],[origin(3) ez_imu(3)],'b-^', 'LineWidth',3);
    
    plot3([origin(1) v(1)/v_len],[origin(2) v(2)],[origin(3) v(3)],'c-^', 'LineWidth',3);
    xlim([-1 1])
    ylim([-1 1])
    zlim([-1 1])
    grid on;
    hold off
    
    pause(.15)

end




% [Xc0,Yc0,Zc0] = cylinder([0 1])

%    for i = 1:N
%     figure(1)  
%     imshow(processo(:,:,1,i))
%       hold on
%       plot(X,Y,'o')
%       plot(X0,Y0,'o')
%       plot(X1,Y1,'o')
%       plot(X2,Y2,'o')
%       plot(X3,Y3,'o')
%       hold off
%       F(i) = getframe(gcf) ;
%       drawnow
%     end
%   % create the video writer with 1 fps
%   writerObj = VideoWriter('myVideo.avi');
%   writerObj.FrameRate = 10;
%   % set the seconds per image
% % open the video writer
% open(writerObj);
% % write the frames to the video
% for i=1:length(F)
%     % convert the image to a frame
%     frame = F(i) ;    
%     writeVideo(writerObj, frame);
% end
% % close the writer object
% close(writerObj);

function [alpha, beta, gamma] = getVelAngles(Vel)
    alpha = atan2(sqrt(Vel(2)^2+Vel(3)^2),Vel(1));
    beta = atan2(sqrt(Vel(3)^2+Vel(1)^2),Vel(2));
    gamma = atan2(sqrt(Vel(1)^2+Vel(2)^2),Vel(3));
end