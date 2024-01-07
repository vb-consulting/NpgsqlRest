create function case_void() 
returns void 
language plpgsql
as 
$$
begin
    raise info 'case_void';
end;
$$;

create function case_return_int(_i int) 
returns int 
language plpgsql
as 
$$
begin
    raise info '_i = %', _i;
    return _i;
end;
$$;

create function case_return_text(_t text) 
returns text 
language plpgsql
as 
$$
begin
    raise info '_t = %', _t;
    return _t;
end;
$$;

create function case_return_json(_json json) 
returns json 
language plpgsql
as 
$$
begin
    raise info '_t = %', _json;
    return _json;
end;
$$;

create function case_return_setof_int() 
returns setof int 
language plpgsql
as 
$$
begin
    return query select i from (values (1), (2), (3)) t(i);
end;
$$;


create function case_return_setof_text() 
returns setof text 
language plpgsql
as 
$$
begin
    return query select i from (values ('ABC'), ('XYZ'), ('IJN')) t(i);
end;
$$;

create function case_return_setof_json() 
returns setof json 
language plpgsql
as 
$$
begin
    return query select j from (
        values 
            (json_build_object('A', 1)),
            (json_build_object('B', 'XY')),
            (json_build_object('C', true)),
            (json_build_object('D', null))
    ) t(j);
end;
$$;
