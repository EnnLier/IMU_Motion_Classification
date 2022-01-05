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

formatspec = '%s %f:%f:%f %f %f %f %f %f %f %f %f %f';
dat = [];
for i = 1 : length(names)
    id = fopen(files{i,1});
    tmp = textscan(id,formatspec);
    tmp2 = [];
    for k = 2 : length(tmp)
        tmp2 = [tmp2 tmp{1,k}(:,1)];
    end
    dat = [dat; tmp2];
end