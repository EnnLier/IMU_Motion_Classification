clc
clear
close all

rotx = @(t) [1 0 0; 0 cos(t) -sin(t) ; 0 sin(t) cos(t)] ;
roty = @(t) [cos(t) 0 sin(t) ; 0 1 0 ; -sin(t) 0  cos(t)] ;
rotz = @(t) [cos(t) -sin(t) 0 ; sin(t) cos(t) 0 ; 0 0 1] ;

% load("Messdaten_01-06-2022 13-08.mat");
% load('Messdaten_01-29-2022 19-08.mat'); % Noise
% load("Messdaten_03-20-2022 17-26.mat");
% load('Messdaten_03-21-2022 15-00.mat');
% load("Messdaten_03-21-2022 16-27.mat"); %Rotation um Z
load("Messdaten_03-21-2022 16-32.mat");


% addpath("Filter");

%% Plot Koordinatensystem
acc = zeros(size(data.Acc));
gyr = acc;
for i = 1 : length(data.t)
    if isMoving(data.Acc_filt(i,:),data.Gyr(i,:),.8)
        acc(i,:) = data.Acc_filt(i,:);
        gyr(i,:) = data.Gyr(i,:);
    else
        acc(i,:) = [0 0 0];
        gyr(i,:) = [0 0 0];
    end
end
% figure
% subplot(3,1,1)
% plot(data.Acc(:,1))
% title("Kartesische Beschleunigung")
% hold on
% plot(acc(:,1))
% 
% subplot(3,1,2)
% plot(data.Acc(:,2))
% hold on
% plot(acc(:,2))
% 
% subplot(3,1,3)
% plot(data.Acc(:,3))
% hold on
% plot(acc(:,3))
% 
% 
% figure
% subplot(3,1,1)
% plot(data.Gyr(:,1))
% title("Winkelgeschwindigkeit")
% hold on
% plot(gyr(:,1))
% 
% subplot(3,1,2)
% plot(data.Gyr(:,2))
% hold on
% plot(gyr(:,2))
% 
% subplot(3,1,3)
% plot(data.Gyr(:,3))
% hold on
% plot(gyr(:,3))

%%
v = zeros(size(data.Vel));
% v = data.Vel;
v(:,1) = cumtrapz(data.t(:),acc(:,1));
v(:,2) = cumtrapz(data.t(:),acc(:,2));
v(:,3) = cumtrapz(data.t(:),acc(:,3));


a = acc;
% v(abs(v) <= 0.02) = 0;
v_angles = zeros(size(v));
v_len = zeros(size(v,1),1);
a_len = v_len;
R_KSimu_KSv = zeros(3,3,length(v));
R_KSimu_KSa = R_KSimu_KSv;

for i = 1 : length(v)
    [alphav, betav] = getVelAngles(v(i,:)');
%     [alphaa, betaa, gammaa] = getVelAngles(a(i,:)');
%     R_KSimu_KSv(:,:,i) = rotz(gammav)*roty(betav)*rotx(alphav);
    R_KSimu_KSv(:,:,i) = roty(betav)*rotz(alphav);
%     R_KSimu_KSa(:,:,i) = rotz(gammaa)*roty(betaa)*rotx(alphaa);
    v_len(i,1) = norm(v(i,:)');
%     a_len(i,1) = norm(a(i,:)');
    v(i,:) = v(i,:)/v_len(i);
%     a(i,:) = a(i,:)/a_len(i);
end
% v = v./max(v_len);
v(isnan(v)) = 0;
% a(isnan(a)) = 0;
%%


ex0 = [1 0 0]';
ey0 = [0 1 0]';
ez0 = [0 0 1]';
origin = [0,0,0];



for i = 200 : 10 :length(data.t)
    q = quaternion(data.Qw(i,1),data.Qx(i,1),data.Qy(i,1),data.Qz(i,1));
    
    
%     q.normalize();
    R_imu = quat2rotm(q);
    R = eye(3);
    R_v = R_KSimu_KSv(:,:,i);
%     e = quat2eul(q);
%     R = eul2rotm(e);
    ex_welt = (R * ex0);
    ey_welt = (R * ey0);
    ez_welt = (R * ez0);
    
    ex_imu = (R_imu * ex0);
    ey_imu = (R_imu * ey0);
    ez_imu = (R_imu * ez0);
    
    ex_board = (R_v * ex_imu);
    ey_board = (R_v * ey_imu);
    ez_board = (R_v * ez_imu);
    
    v_i = transpose(inv(R_imu)) * v(i,:)';
    
    text(-0.8,1,0,num2str(data.t(i)));
    
    figure(1);
    plot3([origin(1) ex_welt(1)],[origin(2) ex_welt(2)],[origin(3) ex_welt(3)],'k-^', 'LineWidth',1);
    hold on;
    plot3([origin(1) ey_welt(1)],[origin(2) ey_welt(2)],[origin(3) ey_welt(3)],'k-^', 'LineWidth',1);
    plot3([origin(1) ez_welt(1)],[origin(2) ez_welt(2)],[origin(3) ez_welt(3)],'k-^', 'LineWidth',1);
    
    plot3([origin(1) ex_imu(1)],[origin(2) ex_imu(2)],[origin(3) ex_imu(3)],'r-^', 'LineWidth',1);
    plot3([origin(1) ey_imu(1)],[origin(2) ey_imu(2)],[origin(3) ey_imu(3)],'g-^', 'LineWidth',1);
    plot3([origin(1) ez_imu(1)],[origin(2) ez_imu(2)],[origin(3) ez_imu(3)],'b-^', 'LineWidth',1);
    
    plot3([origin(1) v_i(1)],[origin(2) v_i(2)],[origin(3) v_i(3)],'c-^', 'LineWidth',3);
%     plot3([origin(1) a(i,1)],[origin(2) a(i,2)],[origin(3) a(i,3)],'m-^', 'LineWidth',3);
    
%     plot3([origin(1) ex_board(1)],[origin(2) ex_board(2)],[origin(3) ex_board(3)],'r-^', 'LineWidth',3);
%     plot3([origin(1) ey_board(1)],[origin(2) ey_board(2)],[origin(3) ey_board(3)],'g-^', 'LineWidth',3);
%     plot3([origin(1) ez_board(1)],[origin(2) ez_board(2)],[origin(3) ez_board(3)],'b-^', 'LineWidth',3);
    
    
    xlim([-1 1])
    ylim([-1 1])
    zlim([-1 1])
    grid on;
    hold off
    
    pause(.02)

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

% function [alpha, beta, gamma] = getVelAngles(Vel)
%     alpha = atan2(sqrt(Vel(2)^2+Vel(3)^2),Vel(1));  
%     beta = atan2(sqrt(Vel(3)^2+Vel(1)^2),Vel(2));  
%     gamma = atan2(sqrt(Vel(1)^2+Vel(2)^2),Vel(3));
% end

function [alpha, beta] = getVelAngles(Vel)
    alpha = -atan2(Vel(2),Vel(1));  %rotation um z
    beta = atan2(Vel(3),sqrt(Vel(2)^2+Vel(1)^2));   %Rotation um y
%     gamma = atan2(sqrt(Vel(1)^2+Vel(2)^2),Vel(3));
end

function rotating = isRotating(gyr,thresh)
    if abs(gyr(1)) < thresh || abs(gyr(2)) < thresh || abs(gyr(3)) < thresh
        rotating = true;
    else
        rotating = false;
    end
%     rotating
end

function accelerating = isAccelerating(acc,thresh)
    if abs(acc(1)) > thresh || abs(acc(2)) > thresh || abs(acc(3)) > thresh
        accelerating = true;
    else
        accelerating = false;
    end
%     accelerating
end

function moving = isMoving(gyr,acc, thresh)
    if isAccelerating(acc,thresh) || isRotating(gyr, thresh)
        moving = true;
    else
        moving = false;
    end
end



