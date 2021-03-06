﻿module MinCamlSharp.Compiler.Closure

type closure = { entry : Id.l; actual_fv : Id.t list }
type t =
    | Unit
    | Int of int
    | Float of float
    | Neg of Id.t
    | Add of Id.t * Id.t
    | Sub of Id.t * Id.t
    | FNeg of Id.t
    | FAdd of Id.t * Id.t
    | FSub of Id.t * Id.t
    | FMul of Id.t * Id.t
    | FDiv of Id.t * Id.t
    | IfEq of Id.t * Id.t * t * t
    | IfLE of Id.t * Id.t * t * t
    | Let of (Id.t * Type.t) * t * t
    | Var of Id.t
    | MakeCls of (Id.t * Type.t) * closure * t
    | AppCls of Id.t * Id.t list
    | AppDir of Id.l * Id.t list
    | Tuple of Id.t list
    | LetTuple of (Id.t * Type.t) list * Id.t * t
    | Get of Id.t * Id.t
    | Put of Id.t * Id.t * Id.t
    | ExtArray of Id.l

type fundef = { name : Id.l * Type.t;
                args : (Id.t * Type.t) list;
                formal_fv : (Id.t * Type.t) list;
                body : t }

type prog = Prog of fundef list * t

let rec fv = function
    | Unit | Int(_) | Float(_) | ExtArray(_) -> Set.empty
    | Neg(x) | FNeg(x) -> Set.singleton x
    | Add(x, y) | Sub(x, y) | FAdd(x, y) | FSub(x, y) | FMul(x, y) | FDiv(x, y) | Get(x, y) -> Set.ofList [x; y]
    | IfEq(x, y, e1, e2)| IfLE(x, y, e1, e2) -> Set.add x (Set.add y (Set.union (fv e1) (fv e2)))
    | Let((x, t), e1, e2) -> Set.union (fv e1) (Set.remove x (fv e2))
    | Var(x) -> Set.singleton x
    | MakeCls((x, t), { entry = l; actual_fv = ys }, e) -> Set.remove x (Set.union (Set.ofList ys) (fv e))
    | AppCls(x, ys) -> Set.of_list (x :: ys)
    | AppDir(_, xs) | Tuple(xs) -> Set.of_list xs
    | LetTuple(xts, y, e) -> Set.add y (Set.difference (fv e) (Set.ofList (List.map fst xts)))
    | Put(x, y, z) -> Set.of_list [x; y; z]

let (toplevel : fundef list ref) = ref []

let rec g env known = function
    | KNormal.Unit -> Unit
    | KNormal.Int(i) -> Int(i)
    | KNormal.Float(d) -> Float(d)
    | KNormal.Neg(x) -> Neg(x)
    | KNormal.Add(x, y) -> Add(x, y)
    | KNormal.Sub(x, y) -> Sub(x, y)
    | KNormal.FNeg(x) -> FNeg(x)
    | KNormal.FAdd(x, y) -> FAdd(x, y)
    | KNormal.FSub(x, y) -> FSub(x, y)
    | KNormal.FMul(x, y) -> FMul(x, y)
    | KNormal.FDiv(x, y) -> FDiv(x, y)
    | KNormal.IfEq(x, y, e1, e2) -> IfEq(x, y, g env known e1, g env known e2)
    | KNormal.IfLE(x, y, e1, e2) -> IfLE(x, y, g env known e1, g env known e2)
    | KNormal.Let((x, t), e1, e2) -> Let((x, t), g env known e1, g (Map.add x t env) known e2)
    | KNormal.Var(x) -> Var(x)
    | KNormal.LetRec({ KNormal.name = (x, t); KNormal.args = yts; KNormal.body = e1 }, e2) ->    
        let toplevel_backup = !toplevel in
        let env' = Map.add x t env in
        let known' = Set.add x known in
        let e1' = g (Map.add_list yts env') known' e1 in
        let zs = Set.difference (fv e1') (Set.ofList (List.map fst yts)) in
        let known', e1' =
            if Set.isEmpty zs then known', e1' else
            (Printf.eprintf "free variable(s) %s found in function %s@." (Id.pp_list (Set.toList zs)) x;
             Printf.eprintf "function %s cannot be directly applied in fact@." x;
             toplevel := toplevel_backup;
             let e1' = g (Map.add_list yts env') known e1 in
             known, e1') in
        let zs = Set.toList (Set.difference (fv e1') (Set.add x (Set.ofList (List.map fst yts)))) in
        let zts = List.map (fun z -> (z, Map.find z env')) zs in
        toplevel := { name = (Id.L(x), t); args = yts; formal_fv = zts; body = e1' } :: !toplevel;
        let e2' = g env' known' e2 in
        if Set.contains x (fv e2') then
            MakeCls((x, t), { entry = Id.L(x); actual_fv = zs }, e2')
        else
            (Printf.eprintf "eliminating closure(s) %s@." x;
             e2')
    | KNormal.App(x, ys) when Set.contains x known ->
        Printf.eprintf "directly applying %s@." x;
        AppDir(Id.L(x), ys)
    | KNormal.App(f, xs) -> AppCls(f, xs)
    | KNormal.Tuple(xs) -> Tuple(xs)
    | KNormal.LetTuple(xts, y, e) -> LetTuple(xts, y, g (Map.add_list xts env) known e)
    | KNormal.Get(x, y) -> Get(x, y)
    | KNormal.Put(x, y, z) -> Put(x, y, z)
    | KNormal.ExtArray(x) -> ExtArray(Id.L(x))
    | KNormal.ExtFunApp(x, ys) -> AppDir(Id.L("min_caml_" + x), ys)

let transform e =
    toplevel := [];
    let e' = g Map.empty Set.empty e in
    Prog(List.rev !toplevel, e')