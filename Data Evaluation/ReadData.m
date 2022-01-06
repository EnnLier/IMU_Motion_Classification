clc
clear 

Filename = sprintf('Messdaten_%s.mat', datestr(now,'mm-dd-yyyy HH-MM'));

% len = length(dir('Data/*.txt'));
% folder = "results/";
% name = "measurement_data_";
% datatype = ".mat";

names = dir("Data");
names(1) =[];
names(1) =[];

temp = struct2cell(names)';
names = temp(:,1);
files = {};
for i = 1 : length(names)
    nam = names{i,1};
    files =    [files; strcat('Data/',names{i,1})];
end
% 

% formatspec = '%s %f:%f:%f %f %f %f %f %f %f %f %f %f';
formatspec = '%s %s %f %f %f %f %f %f %f %f %f';
dat = [];
dT = [];
for i = 1 : length(names)
    id = fopen(files{i,1});
    tmp = textscan(id,formatspec);
    dT = [dT; getDatetime(tmp)];
    tmp(:,[1,2]) = [];
%     tmp{1,1} = [];
    tmp2 = [];
    for k = 3 : length(tmp)
        tmp2 = [tmp2 tmp{1,k}(:,1)];
    end
    dat = [dat; tmp2];
end

data.t = datetimeToTimestamp(dT);
data.dt = dT;
data.Qw = dat(:,1);
data.Qx = dat(:,2);
data.Qy = dat(:,3);
data.Qz = dat(:,4);
data.Accx = dat(:,5);
data.Accy = dat(:,6);
data.Accz = dat(:,7);

save(Filename,'data');

function dt = getDatetime(cArr)
%     tmp = [];
%     tmp = zeros(size(cArr{1,1}));
%     dT = zeros(size(cArr{1,1}));
    dt = [];
    for i = 1 : length(cArr{1,1})
        tmp = strcat(cArr{1,1}(i,1)," ", cArr{1,2}(i,1));
        dt = [dt; datetime(tmp,'Format','yy/dd/MM HH:mm:ss.SSSSSSS') ];
    end
end

function rt = datetimeToTimestamp(dtArr)
    rt = zeros(length(dtArr),1);
%     rt(1,1) = 0;
    for i = 1 : length(dtArr)
%         t1 = dtArr(i,1);
%         t2 = dtArr(i+1,1);
        rt(i,1) = seconds(dtArr(i,1)-dtArr(1,1));
    end
end