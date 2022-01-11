clc
clear
close all

load('Ruhemessung.mat')

addpath('Filter')
% load('Z1.mat')

%%


% dat = data.Accy(:,1);
dat = data.Acc(:,1);

figure
subplot(311)
plot(dat)


subplot(312)
mu = mean(dat);    % data 
sd = std(dat);     % data std
ha = histfit(dat,10);
yt = get(gca, 'YTick');
set(gca, 'YTick', yt, 'YTickLabel', round(yt/numel(dat),4))
% set(gca,'xticklabel',num2str(tix,'%.1f'))
set(ha(1),'facecolor',[0 0 1]); set(ha(2),'color',[1 .5 0])
xlim([-0.1 0.1])


subplot(313)
[fx, mx, px] = calcFFT(133,length(dat),dat-mu);
plot(fx,mx)

figure
plot(dat)
hold on
dat = filter(BW_Lowpass,dat);
plot(dat)
vel_f = cumtrapz(data.t(:,1),dat);

figure
plot(data.Velx(:,1))
hold on
plot(vel_f)


function [f,P1,phase] = calcFFT(Fs, L, X)
%     Fs = 1000;            % Sampling frequency                    
%     T = 1/Fs;             % Sampling period       
%     L = 1500;             % Length of signal
%     t = (0:L-1)*T;        % Time vector
    Y = fft(X);    
    tol = 0.02;
%     z(abs(z) < tol) = 0;
%     phase = angle(z(1:L/2+1)/L);
    
    P2 = abs(Y/L);
    phase = angle(Y/L);
    phase = 2*phase(1:round(L/2)+1);
    
    P1 = P2(1:round(L/2)+1);
    P1(2:end-1) = 2*P1(2:end-1);
    f = Fs*(0:round(L/2))/L;
    phase(P1<tol)=0;
end